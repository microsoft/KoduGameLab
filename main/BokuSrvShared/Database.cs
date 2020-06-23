using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;

namespace BokuSrvShared
{
    /// <summary>
    /// Abstract base class for managing a database connection.
    /// </summary>
    public abstract class Database
    {
        #region Protected
        protected abstract string ConnectString { get; }
        protected abstract string ProcedureName(int id);

        protected class ParameterSpec
        {
            public string name;
            public SqlDbType type;
            public ParameterDirection direction;

            public ParameterSpec(
                string name,
                SqlDbType type,
                ParameterDirection direction)
            {
                this.name = name;
                this.type = type;
                this.direction = direction;
            }
        }

        protected class ProcedureSpec
        {
            public ParameterSpec[] parms;

            public ProcedureSpec(ParameterSpec[] parms)
            {
                this.parms = parms;
            }
        }

        private Dictionary<int, ProcedureSpec> procedures = new Dictionary<int, ProcedureSpec>();

        protected SqlParameter[] BindProcedure(int id, SqlCommand cmd, object[] args)
        {
            UnbindProcedure(cmd);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = ProcedureName(id);

            SqlParameter[] arr = new SqlParameter[args.Length];
            ProcedureSpec spec = procedures[id];

            for (int i = 0; i < args.Length; ++i)
            {
                arr[i] = new SqlParameter();
                arr[i].ParameterName = spec.parms[i].name;
                arr[i].SqlDbType = spec.parms[i].type;
                arr[i].Direction = spec.parms[i].direction;
                arr[i].Value = args[i];
                cmd.Parameters.Add(arr[i]);
            }

            return arr;
        }

        protected void UnbindProcedure(SqlCommand cmd)
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = String.Empty;
            cmd.Parameters.Clear();
        }

        protected void InitProcedure(int id, ParameterSpec[] parms)
        {
            if (!procedures.ContainsKey(id))
            {
                procedures.Add(id, new ProcedureSpec(parms));
            }
        }

        protected SqlConnection GetSqlConnection(int connectionTimeout = 30)
        {
            return GetSqlConnection(connectionTimeout, true);
        }

        protected SqlConnection GetSqlConnection(int connectionTimeout, bool allowFailover)
        {
            string connectString = ConnectString + ";Connection Timeout=" + connectionTimeout;
            SqlConnection conn = new SqlConnection(connectString);

            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            return conn;
        }

        protected static byte[] GetByteArray(SqlDataReader reader, int field)
        {
            if (reader.IsDBNull(field))
                return null;

            long length = reader.GetBytes(field, 0, null, 0, 0);
            byte[] result = new byte[length];
            reader.GetBytes(field, 0, result, 0, result.Length);
            return result;
        }

        #endregion
    }
}
