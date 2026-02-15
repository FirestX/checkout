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

	public static void DeleteDatabase(string connectionString)
	{
		// Extract the database file path from the connection string
		var connectionParts = connectionString.Split(';');
		string? dbPath = null;

		foreach (var part in connectionParts)
		{
			if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
			{
				dbPath = part.Substring("Data Source=".Length).Trim();
				break;
			}
		}

		if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
		{
			File.Delete(dbPath);
		}
	}
}