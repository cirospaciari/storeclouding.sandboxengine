using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.DAO
{
    /// <summary>
    /// Gerencia a abertura e fechamento de conexões com o banco de dados para melhor performance
    /// </summary>
    class ConnectionManagerApplication : IApplication
    {
        private const int ConnectionPoolSize = 10000;
        private const int ConnectionsLimit = 30000;

        private Queue<SqlConnection> ConnectionPool = new Queue<SqlConnection>(ConnectionPoolSize);
        private Queue<SqlConnection> ConnectionClosePool = new Queue<SqlConnection>(ConnectionPoolSize);

        private List<Threading.ApplicationThread> threads = new List<Threading.ApplicationThread>();
        public ConnectionManagerApplication()
        {
        }

        public SqlConnection OpenConnection()
        {
            SqlConnection connection = null;

            lock (ConnectionPool)
            {
                try
                {
                    connection = ConnectionPool.Dequeue();
                }
                catch (Exception) { }//caso esteja vazia
            }
            if (connection == null)
            {
                connection = new SqlConnection(GameApplication.ConnectionString);

                if (ConnectionClosePool.Count > ConnectionsLimit)
                    WaitCloseOne();

                connection.Open();
            }
            return connection;
        }

        public void CloseConnection(SqlConnection connection)
        {
            lock (ConnectionClosePool)
            {
                ConnectionClosePool.Enqueue(connection);
            }
        }

        private void WaitCloseOne()
        {
            while (ConnectionClosePool.Count > ConnectionsLimit)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        private void OpenConnectionProcess(object obj)
        {
            var connectionLimit = ConnectionClosePool.Count + ConnectionPool.Count < ConnectionsLimit;
            var connectionPool = ConnectionPool.Count < ConnectionPoolSize;
            while (connectionPool && connectionLimit)
            {
                System.Threading.Thread.Sleep(100);

                var connection = new SqlConnection(GameApplication.ConnectionString);
                connection.Open();
                lock(ConnectionPool)
                {
                    ConnectionPool.Enqueue(connection);
                }
            }
        }

        private void CloseConnectionProcess(object obj)
        {
            SqlConnection connection = null;
            while (true)
            {

                lock (ConnectionClosePool)
                {
                    try
                    {
                        connection = ConnectionClosePool.Dequeue();
                    }
                    catch (Exception) { }//caso esteja vazia
                }
                if (connection == null)
                    break;

                try
                {
                    if (connection.State != System.Data.ConnectionState.Closed)
                        connection.Close();

                }
                catch (Exception){}
            }
        }

        public bool Start(out string error)
        {
            error = null;
            try
            {
                var closePoolUpdate = new Threading.ApplicationThread(1000);
                closePoolUpdate.OnRun += CloseConnectionProcess;
                threads.Add(closePoolUpdate);
                closePoolUpdate.Start(this);

                var openPoolUpdate = new Threading.ApplicationThread(1000);
                openPoolUpdate.OnRun += OpenConnectionProcess;
                threads.Add(openPoolUpdate);
                openPoolUpdate.Start(this);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }

        }

        public bool Update(out string error)
        {
            error = null;
            return true;
        }

        public bool Stop(out string error)
        {
            error = null;
            try
            {
                foreach (var thread in threads)
                    thread.Stop();

                foreach (var thread in threads)
                    thread.Join();
                
                return true;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
        }
    }
}
