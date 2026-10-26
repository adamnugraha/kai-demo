using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using RailBooking.Core;
namespace RailBooking.Legacy {
 public class RailController:Controller {
  string Setting(string key)=>Environment.GetEnvironmentVariable(key)??ConfigurationManager.AppSettings[key];
  Store Db()=>new Store(()=>new SqlConnection(Setting("DB_CONNECTION")),false);
  bool Allowed()=>!String.IsNullOrEmpty(Setting("LAB_TOKEN"))&&Request.Headers["X-Lab-Token"]==Setting("LAB_TOKEN");
  ContentResult Output(object value,int status=200){Response.StatusCode=status;Response.Headers["X-App-Version"]="legacy-net48";return Content(new JavaScriptSerializer().Serialize(value),"application/json");}
  public ActionResult Trains(){if(!Allowed())return new HttpStatusCodeResult(401);try{return Output(Db().Trains().ConvertAll(t=>new{id=t.Id,route=t.Route,remaining=t.Remaining}));}catch{return Output(new{error="Legacy database unavailable"},503);}}
  public async Task<ActionResult> Bookings(string id){
   if(!Allowed())return new HttpStatusCodeResult(401);
   try{
    if(Request.HttpMethod=="GET"){var found=Db().Find(id);return found==null?Output(new{error="Not found"},404):Output(new{id=found.Id,trainId=found.TrainId,seats=found.Seats,replay=false});}
    if(Request.HttpMethod!="POST")return new HttpStatusCodeResult(405);
    var input=new JavaScriptSerializer().Deserialize<BookingRequest>(new StreamReader(Request.InputStream).ReadToEnd());
    var result=Db().Book(input,Request.Headers["Idempotency-Key"]);
    // Intentional baseline coupling: a notification outage returns 503 even after commit.
    using(var client=new HttpClient{Timeout=TimeSpan.FromSeconds(3)}){
     client.DefaultRequestHeaders.Add("X-Lab-Token",Setting("LAB_TOKEN"));
     var body=new JavaScriptSerializer().Serialize(new{eventId=result.Id,bookingId=result.Id});
     using(var r=await client.PostAsync(Setting("NOTIFIER_URL").TrimEnd('/')+"/notify",new StringContent(body,Encoding.UTF8,"application/json")))r.EnsureSuccessStatusCode();
    }
    return Output(new{id=result.Id,trainId=result.TrainId,seats=result.Seats,replay=result.Replay},result.Replay?200:201);
   }catch(RuleException e){return Output(new{error=e.Message},e.Status);}catch{return Output(new{error="Legacy dependency failure. Check outcome and retry with the same key."},503);}
  }
 }
}
