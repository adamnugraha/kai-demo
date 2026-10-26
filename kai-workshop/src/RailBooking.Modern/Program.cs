using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Data.Sqlite;
using RailBooking.Core;

var builder=WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();builder.Logging.AddJsonConsole();
string Env(string n,string fallback="")=>Environment.GetEnvironmentVariable(n)??fallback;
var provider=Env("DB_PROVIDER","sqlite");
var connection=Env("DB_CONNECTION","Data Source=railbooking.db;Default Timeout=30");
var token=Env("LAB_TOKEN");
if(token.Length<24)throw new InvalidOperationException("LAB_TOKEN must be at least 24 characters. Generate it with ops/new-env.py.");
var store=new Store(()=>provider=="sqlserver"?new Microsoft.Data.SqlClient.SqlConnection(connection):new SqliteConnection(connection),provider!="sqlserver");
if(args.Contains("--init-db")){store.Initialize();Console.WriteLine("Database schema initialized.");return;}
if(Env("AUTO_INIT","false")=="true")store.Initialize();
var client=new HttpClient{Timeout=TimeSpan.FromSeconds(3)};
client.DefaultRequestHeaders.Add("X-Lab-Token",token);
var notifier=Env("NOTIFIER_URL","http://localhost:8082").TrimEnd('/');
async Task<int> Dispatch(){int delivered=0;foreach(var e in store.Pending()){
 try{using var response=await client.PostAsJsonAsync(notifier+"/notify",new {eventId=e.Id,bookingId=e.BookingId});if(response.IsSuccessStatusCode){store.Complete(e.Id);delivered++;}else store.Failed(e);}
 catch(HttpRequestException){store.Failed(e);}catch(TaskCanceledException){store.Failed(e);}
}return delivered;}
if(args.Contains("--dispatch-once")){Console.WriteLine("Delivered "+await Dispatch());return;}
var app=builder.Build();app.Urls.Add("http://0.0.0.0:"+Env("PORT","8080"));
var meters=new Metrics();var release=Env("APP_VERSION","modern-v1");
var fault=Math.Clamp(int.TryParse(Env("FAULT_PERCENT","0"),out var f)?f:0,0,100);
var legacy=Env("LEGACY_URL").TrimEnd('/');var stage=Env("MIGRATION_STAGE","modern");
app.Use(async(ctx,next)=>{
 bool protectedPath=ctx.Request.Path.StartsWithSegments("/api")||ctx.Request.Path=="/metrics";
 if(protectedPath && ctx.Request.Headers["X-Lab-Token"]!=token && ctx.Request.Headers.Authorization!="Bearer "+token){ctx.Response.StatusCode=401;return;}
 bool measured=ctx.Request.Path.StartsWithSegments("/api/trains")||ctx.Request.Path.StartsWithSegments("/api/bookings");
 var sw=Stopwatch.StartNew();ctx.Response.Headers["X-App-Version"]=release;ctx.Response.Headers["X-Request-ID"]=ctx.TraceIdentifier;
 try{
  if(measured && Random.Shared.Next(100)<fault){ctx.Response.StatusCode=503;await ctx.Response.WriteAsJsonAsync(new {error="Intentional lab fault",version=release});return;}
  // Explicit route adapter for this small demo. Use YARP for a general-purpose migration proxy.
  bool proxy=legacy!="" && measured && (stage=="legacy" || (stage=="search" && !ctx.Request.Path.StartsWithSegments("/api/trains")));
  if(proxy){using var req=new HttpRequestMessage(new HttpMethod(ctx.Request.Method),legacy+ctx.Request.Path+ctx.Request.QueryString);if(ctx.Request.ContentLength>0){req.Content=new StreamContent(ctx.Request.Body);req.Content.Headers.ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");}if(ctx.Request.Headers.TryGetValue("Idempotency-Key",out var key))req.Headers.TryAddWithoutValidation("Idempotency-Key",key.ToString());using var res=await client.SendAsync(req);ctx.Response.StatusCode=(int)res.StatusCode;ctx.Response.ContentType="application/json";ctx.Response.Headers["X-Backend"]="legacy-net48";await res.Content.CopyToAsync(ctx.Response.Body);return;}
  ctx.Response.Headers["X-Backend"]="modern-net10";await next(ctx);
 }catch(RuleException e){ctx.Response.StatusCode=e.Status;await ctx.Response.WriteAsJsonAsync(new {error=e.Message});}
 catch(Exception e){app.Logger.LogError(e,"Request failed {RequestId} {Version}",ctx.TraceIdentifier,release);ctx.Response.StatusCode=503;await ctx.Response.WriteAsJsonAsync(new {error="Temporary failure. Retry with the same idempotency key.",requestId=ctx.TraceIdentifier});}
 finally{if(measured){meters.Observe(ctx.Response.StatusCode,sw.Elapsed.TotalSeconds);app.Logger.LogInformation("journey outcome {RequestId} {Version} {Status} {ElapsedMs}",ctx.TraceIdentifier,release,ctx.Response.StatusCode,sw.ElapsedMilliseconds);}}
});
app.UseDefaultFiles();app.UseStaticFiles();
app.MapGet("/health/live",()=>Results.Ok(new{status="alive",version=release}));
app.MapGet("/health/ready",()=>{store.Trains();return Results.Ok(new{status="ready"});});
app.MapGet("/api/trains",()=>store.Trains());
app.MapPost("/api/bookings",async(BookingRequest b,HttpContext ctx)=>{
 var booked=store.Book(b,ctx.Request.Headers["Idempotency-Key"].ToString());
 if(Env("NOTIFICATION_MODE","async")=="sync"){
  using var r=await client.PostAsJsonAsync(notifier+"/notify",new{eventId=booked.Id,bookingId=booked.Id});r.EnsureSuccessStatusCode();
 }
 return Results.Json(booked,statusCode:booked.Replay?200:201);
});
app.MapGet("/api/bookings/{id}",(string id)=>store.Find(id) is Booking b?Results.Ok(b):Results.NotFound());
app.MapGet("/api/ops",()=>new {version=release,provider,migrationStage=stage,counts=store.Snapshot(),metrics=meters.Snapshot()});
app.MapPost("/api/ops/dispatch",async()=>new {delivered=await Dispatch()});
app.MapGet("/metrics",()=>Results.Text(meters.Prometheus(),"text/plain; version=0.0.4"));
if(Env("RUN_WORKER","false")=="true"){
 app.Lifetime.ApplicationStarted.Register(()=>_ = Task.Run(async()=>{while(!app.Lifetime.ApplicationStopping.IsCancellationRequested){try{await Dispatch();}catch(Exception e){app.Logger.LogError(e,"Outbox worker failed");}try{await Task.Delay(2000,app.Lifetime.ApplicationStopping);}catch(OperationCanceledException){break;}}}));
}
await app.RunAsync();

class Metrics {
 long eligible,bad,under1s;readonly ConcurrentDictionary<string,long> statuses=new();
 public void Observe(int status,double sec){statuses.AddOrUpdate(status.ToString(),1,(_,n)=>n+1);if(status>=200 && status<300 || status>=500){Interlocked.Increment(ref eligible);if(status>=500)Interlocked.Increment(ref bad);if(status<300 && sec<=1)Interlocked.Increment(ref under1s);}}
 public object Snapshot()=>new{eligible=Interlocked.Read(ref eligible),bad=Interlocked.Read(ref bad),goodUnder1s=Interlocked.Read(ref under1s)};
 public string Prometheus(){var s=new StringBuilder("# TYPE rail_eligible_total counter\nrail_eligible_total "+Interlocked.Read(ref eligible)+"\n# TYPE rail_bad_total counter\nrail_bad_total "+Interlocked.Read(ref bad)+"\n# TYPE rail_good_under_1s_total counter\nrail_good_under_1s_total "+Interlocked.Read(ref under1s)+"\n");foreach(var p in statuses)s.Append("rail_http_total{status=\"").Append(p.Key).Append("\"} ").Append(p.Value).Append('\n');return s.ToString();}
}
