using Microsoft.Data.Sqlite;
var b=WebApplication.CreateBuilder(args);b.Logging.ClearProviders();b.Logging.AddJsonConsole();var app=b.Build();
var token=Environment.GetEnvironmentVariable("LAB_TOKEN")??"";if(token.Length<24)throw new Exception("Set LAB_TOKEN");
app.Urls.Add("http://0.0.0.0:"+(Environment.GetEnvironmentVariable("PORT")??"8082"));
var db=Environment.GetEnvironmentVariable("MOCK_DB")??"Data Source=notifications.db;Default Timeout=30";
using(var c=new SqliteConnection(db)){c.Open();using var q=c.CreateCommand();q.CommandText="CREATE TABLE IF NOT EXISTS Notifications(EventId TEXT PRIMARY KEY, BookingId TEXT NOT NULL)";q.ExecuteNonQuery();}
bool failing=false;
app.Use(async(ctx,next)=>{if(ctx.Request.Path!="/health/live" && ctx.Request.Headers["X-Lab-Token"]!=token){ctx.Response.StatusCode=401;return;}await next(ctx);});
app.MapGet("/health/live",()=>"alive");
app.MapPost("/admin/fault",(Fault f)=>{failing=f.Enabled;return new{failing};});
app.MapPost("/notify",(Notification n)=>{if(failing)return Results.StatusCode(503);if(string.IsNullOrWhiteSpace(n.EventId)||string.IsNullOrWhiteSpace(n.BookingId))return Results.BadRequest();using var c=new SqliteConnection(db);c.Open();using var q=c.CreateCommand();q.CommandText="INSERT OR IGNORE INTO Notifications(EventId,BookingId) VALUES($id,$booking)";q.Parameters.AddWithValue("$id",n.EventId);q.Parameters.AddWithValue("$booking",n.BookingId);return Results.Ok(new{accepted=true,duplicate=q.ExecuteNonQuery()==0});});
app.MapGet("/stats",()=>{using var c=new SqliteConnection(db);c.Open();using var q=c.CreateCommand();q.CommandText="SELECT COUNT(*) FROM Notifications";return new{uniqueNotifications=Convert.ToInt64(q.ExecuteScalar()),failing};});
await app.RunAsync();record Fault(bool Enabled);record Notification(string EventId,string BookingId);
