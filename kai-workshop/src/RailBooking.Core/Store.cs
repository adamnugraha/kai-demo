using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RailBooking.Core {
 public class Train { public string Id {get;set;} public string Route {get;set;} public int Remaining {get;set;} }
 public class BookingRequest { public string TrainId {get;set;} public string PassengerAlias {get;set;} public int Seats {get;set;} }
 public class Booking { public string Id {get;set;} public string TrainId {get;set;} public int Seats {get;set;} public bool Replay {get;set;} }
 public class Event { public string Id {get;set;} public string BookingId {get;set;} public int Attempts {get;set;} }
 public class RuleException:Exception { public int Status {get;} public RuleException(int status,string message):base(message){Status=status;} }
 public class Store {
  readonly Func<DbConnection> factory; readonly bool sqlite;
  public Store(Func<DbConnection> factory,bool sqlite){this.factory=factory;this.sqlite=sqlite;}
  DbConnection Open(){var c=factory();c.Open();return c;}
  static DbCommand Cmd(DbConnection c,string sql,DbTransaction tx=null,params object[] values){
   var q=c.CreateCommand();q.CommandText=sql;q.CommandTimeout=15;q.Transaction=tx;
   for(int i=0;i<values.Length;i+=2){var p=q.CreateParameter();p.ParameterName=(string)values[i];p.Value=values[i+1]??DBNull.Value;q.Parameters.Add(p);}return q;
  }
  static long Now()=>DateTimeOffset.UtcNow.ToUnixTimeSeconds();
  public void Initialize(){
   using(var c=Open()){
    var defs=new Dictionary<string,string>{
     {"Trains","Id varchar(32) PRIMARY KEY, Route nvarchar(160) NOT NULL, Remaining int NOT NULL CHECK(Remaining >= 0)"},
     {"Bookings","Id varchar(36) PRIMARY KEY, IdempotencyKey varchar(96) NOT NULL UNIQUE, PayloadHash varchar(64) NOT NULL, TrainId varchar(32) NOT NULL, Seats int NOT NULL CHECK(Seats > 0), CreatedAt bigint NOT NULL"},
     {"Outbox","Id varchar(36) PRIMARY KEY, BookingId varchar(36) NOT NULL, Delivered int NOT NULL DEFAULT 0, Attempts int NOT NULL DEFAULT 0, CreatedAt bigint NOT NULL, NextAttemptAt bigint NOT NULL DEFAULT 0"}
    };
    foreach(var pair in defs){var sql=sqlite?"CREATE TABLE IF NOT EXISTS "+pair.Key+" ("+pair.Value+")":"IF OBJECT_ID('dbo."+pair.Key+"','U') IS NULL CREATE TABLE dbo."+pair.Key+" ("+pair.Value+")";using(var q=Cmd(c,sql))q.ExecuteNonQuery();}
    foreach(var id in new[]{"DEMO-01","DEMO-02"})using(var q=Cmd(c,"INSERT INTO Trains(Id,Route,Remaining) SELECT @id,@route,50000 WHERE NOT EXISTS(SELECT 1 FROM Trains WHERE Id=@id)",null,"@id",id,"@route",id=="DEMO-01"?"Jakarta - Bandung (synthetic)":"Jakarta - Semarang (synthetic)"))q.ExecuteNonQuery();
   }
  }
  public List<Train> Trains(){using(var c=Open())using(var q=Cmd(c,"SELECT Id,Route,Remaining FROM Trains ORDER BY Id"))using(var r=q.ExecuteReader()){var a=new List<Train>();while(r.Read())a.Add(new Train{Id=r.GetString(0),Route=r.GetString(1),Remaining=Convert.ToInt32(r.GetValue(2))});return a;}}
  public Booking Book(BookingRequest b,string key){
   if(b==null||String.IsNullOrWhiteSpace(b.TrainId)||String.IsNullOrWhiteSpace(b.PassengerAlias)||b.PassengerAlias.Length>64||b.Seats<1||b.Seats>6||String.IsNullOrWhiteSpace(key)||key.Length>96)throw new RuleException(400,"Use a synthetic alias, 1 to 6 seats, and an Idempotency-Key of 1 to 96 characters.");
   string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(b.TrainId+"\n"+b.PassengerAlias+"\n"+b.Seats.ToString(CultureInfo.InvariantCulture)))).Replace("-","");
   using(var c=Open())using(var tx=c.BeginTransaction(IsolationLevel.Serializable)){
    using(var q=Cmd(c,"SELECT Id,TrainId,Seats,PayloadHash FROM Bookings "+(sqlite?"":"WITH (UPDLOCK,HOLDLOCK) ")+"WHERE IdempotencyKey=@key",tx,"@key",key))using(var r=q.ExecuteReader())if(r.Read()){
     if(r.GetString(3)!=hash)throw new RuleException(409,"This idempotency key was already used for different input.");
     var found=new Booking{Id=r.GetString(0),TrainId=r.GetString(1),Seats=Convert.ToInt32(r.GetValue(2)),Replay=true};r.Close();tx.Commit();return found;
    }
    using(var q=Cmd(c,"UPDATE Trains SET Remaining=Remaining-@n WHERE Id=@id AND Remaining>=@n",tx,"@n",b.Seats,"@id",b.TrainId))if(q.ExecuteNonQuery()!=1)throw new RuleException(409,"Train unavailable or insufficient seats.");
    var result=new Booking{Id=Guid.NewGuid().ToString(),TrainId=b.TrainId,Seats=b.Seats};
    using(var q=Cmd(c,"INSERT INTO Bookings(Id,IdempotencyKey,PayloadHash,TrainId,Seats,CreatedAt) VALUES(@id,@key,@hash,@train,@seats,@now)",tx,"@id",result.Id,"@key",key,"@hash",hash,"@train",b.TrainId,"@seats",b.Seats,"@now",Now()))q.ExecuteNonQuery();
    using(var q=Cmd(c,"INSERT INTO Outbox(Id,BookingId,CreatedAt) VALUES(@id,@booking,@now)",tx,"@id",result.Id,"@booking",result.Id,"@now",Now()))q.ExecuteNonQuery();
    tx.Commit();return result;
   }
  }
  public Booking Find(string id){using(var c=Open())using(var q=Cmd(c,"SELECT Id,TrainId,Seats FROM Bookings WHERE Id=@id",null,"@id",id))using(var r=q.ExecuteReader())return r.Read()?new Booking{Id=r.GetString(0),TrainId=r.GetString(1),Seats=Convert.ToInt32(r.GetValue(2))}:null;}
  public List<Event> Pending(){using(var c=Open())using(var q=Cmd(c,(sqlite?"SELECT ":"SELECT TOP (50) ")+"Id,BookingId,Attempts FROM Outbox WHERE Delivered=0 AND NextAttemptAt<=@now ORDER BY CreatedAt"+(sqlite?" LIMIT 50":""),null,"@now",Now()))using(var r=q.ExecuteReader()){var a=new List<Event>();while(r.Read())a.Add(new Event{Id=r.GetString(0),BookingId=r.GetString(1),Attempts=Convert.ToInt32(r.GetValue(2))});return a;}}
  public void Complete(string id){using(var c=Open())using(var q=Cmd(c,"UPDATE Outbox SET Delivered=1 WHERE Id=@id",null,"@id",id))q.ExecuteNonQuery();}
  public void Failed(Event e){using(var c=Open())using(var q=Cmd(c,"UPDATE Outbox SET Attempts=Attempts+1,NextAttemptAt=@next WHERE Id=@id AND Delivered=0",null,"@id",e.Id,"@next",Now()+Math.Min(30,1L<<Math.Min(e.Attempts,5))))q.ExecuteNonQuery();}
  public Dictionary<string,long> Snapshot(){using(var c=Open()){var a=new Dictionary<string,long>();foreach(var t in new[]{"Bookings","Outbox"})using(var q=Cmd(c,"SELECT COUNT(*) FROM "+t))a[t.ToLowerInvariant()]=Convert.ToInt64(q.ExecuteScalar());using(var q=Cmd(c,"SELECT COUNT(*) FROM Outbox WHERE Delivered=0"))a["pending"]=Convert.ToInt64(q.ExecuteScalar());using(var q=Cmd(c,"SELECT MIN(CreatedAt) FROM Outbox WHERE Delivered=0")){var oldest=q.ExecuteScalar();a["oldestPendingSeconds"]=oldest==DBNull.Value?0:Now()-Convert.ToInt64(oldest);}return a;}}
 }
}
