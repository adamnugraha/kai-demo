using Microsoft.Data.Sqlite;using RailBooking.Core;
var sql=Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION");
var file=Path.Combine(Path.GetTempPath(),"rail-tests-"+Guid.NewGuid()+".db");
var store=new Store(()=>sql==null?new SqliteConnection("Data Source="+file+";Default Timeout=30"):new Microsoft.Data.SqlClient.SqlConnection(sql),sql==null);
void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
store.Initialize();var suffix=Guid.NewGuid().ToString();var input=new BookingRequest{TrainId="DEMO-01",PassengerAlias="test",Seats=2};int before=store.Trains()[0].Remaining;
var b=store.Book(input,suffix);var replay=store.Book(input,suffix);Check(b.Id==replay.Id&&replay.Replay,"same key returns same booking");Check(store.Trains()[0].Remaining==before-2,"retry does not consume more seats");
try{store.Book(new BookingRequest{TrainId="DEMO-01",PassengerAlias="changed",Seats=2},suffix);throw new Exception("Expected conflict");}catch(RuleException e){Check(e.Status==409,"key reuse with changed input rejected");}
Check(store.Pending().Any(e=>e.BookingId==b.Id),"booking and outbox event both committed");
long count=store.Snapshot()["bookings"];try{store.Book(new BookingRequest{TrainId="MISSING",PassengerAlias="test",Seats=1},Guid.NewGuid().ToString());throw new Exception("Expected failure");}catch(RuleException){}Check(store.Snapshot()["bookings"]==count,"failed allocation leaves no booking");
var batchKey=Guid.NewGuid().ToString();var parallel=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>Task.Run(()=>store.Book(input,batchKey))));Check(parallel.Select(x=>x.Id).Distinct().Count()==1,"concurrent identical requests create one booking");
var ev=store.Pending().First(e=>e.BookingId==b.Id);store.Failed(ev);Check(!store.Pending().Any(e=>e.Id==ev.Id),"failed event waits before retry");store.Complete(ev.Id);Check(!store.Pending().Any(e=>e.Id==ev.Id),"delivered event is no longer pending");
var tiny="TEST-"+Guid.NewGuid().ToString("N").Substring(0,12);
using(var c=sql==null?(System.Data.Common.DbConnection)new SqliteConnection("Data Source="+file+";Default Timeout=30"):new Microsoft.Data.SqlClient.SqlConnection(sql)){c.Open();using var q=c.CreateCommand();q.CommandText="INSERT INTO Trains(Id,Route,Remaining) VALUES(@id,'Capacity test',3)";var p=q.CreateParameter();p.ParameterName="@id";p.Value=tiny;q.Parameters.Add(p);q.ExecuteNonQuery();}
var attempts=await Task.WhenAll(Enumerable.Range(0,12).Select(_=>Task.Run(()=>{try{store.Book(new BookingRequest{TrainId=tiny,PassengerAlias="capacity-test",Seats=1},Guid.NewGuid().ToString());return true;}catch(RuleException e)when(e.Status==409){return false;}})));
Check(attempts.Count(x=>x)==3 && store.Trains().Single(t=>t.Id==tiny).Remaining==0,"concurrent requests cannot oversell three seats");
Console.WriteLine(sql==null?"SQLite functional tests complete. SQL Server remains a separate integration gate.":"SQL Server integration tests complete.");
