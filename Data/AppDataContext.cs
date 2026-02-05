using LinqToDB;
using CheckOut.Models;
using LinqToDB.Data;

namespace CheckOut.Data;

public class AppDataContext(string connectionString) : DataConnection(connectionString)
{
	public ITable<Teacher> Teachers => this.GetTable<Teacher>();
	public ITable<Device> Devices => this.GetTable<Device>();
	public ITable<CheckIn> CheckIns => this.GetTable<CheckIn>();
}