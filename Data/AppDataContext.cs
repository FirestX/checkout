using LinqToDB;
using CheckOut.Models;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;

namespace CheckOut.Data;

public class AppDataContext(DataOptions options) : DataConnection(options)
{
	public ITable<Teacher> Teachers => this.GetTable<Teacher>();
	public ITable<Device> Devices => this.GetTable<Device>();
	public ITable<CheckIn> CheckIns => this.GetTable<CheckIn>();

	public static void InitializeDatabase(DataOptions dataOptions, string connectionString)
	{
		SQLiteTools.CreateDatabase(connectionString);
		using var db = new AppDataContext(dataOptions);
		db.CreateTable<Teacher>(tableOptions: TableOptions.CreateIfNotExists);
		db.CreateTable<Device>(tableOptions: TableOptions.CreateIfNotExists);
		db.CreateTable<CheckIn>(tableOptions: TableOptions.CreateIfNotExists);
	}
}