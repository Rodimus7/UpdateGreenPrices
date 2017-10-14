using System.Data.SqlClient;

namespace UpdateGreenPrices
{
  public static class TdbConnection
  {
    private static string connectionString = "Data Source=energyretailprofit.database.windows.net;Initial Catalog=EnergyRetailProfit;Integrated Security=False;User ID=energyadmin;Password=Pa$$w0rd;Connect Timeout=60;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
    //private SqlConnection _Sqlconnection = new SqlConnection(connectionString);

    public static SqlConnection DBConn
    { 
      get
      {
        SqlConnection _Sqlconnection = new SqlConnection(connectionString);

        if (_Sqlconnection.State != System.Data.ConnectionState.Open)
          _Sqlconnection.Open();

        return _Sqlconnection;
      }
    }
  }
}