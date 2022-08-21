
using System.Collections.Generic;
using System.Data;

namespace BQJX.DAL.Base
{
    public interface IDataAccessBase
    {

        int ExecuteNonQuery(string sql, Dictionary<string, object> param = null);

        DataTable Query(string sql, Dictionary<string, object> param = null);


    }


}
