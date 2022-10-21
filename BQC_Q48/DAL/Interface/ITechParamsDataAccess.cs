using BQJX.Common;
using System;
using System.Collections.Generic;

namespace Q_Platform.DAL
{
    public interface ITechParamsDataAccess
    {
        TechParams FindTechParamsById(int id);

        TechParams FindTechParamsByName(string techName);

        List<TechParams> GetTechParamsInfoById(int start, int end);

        List<TechParams> GetTechParamsInfoByTechName(string name);

        List<TechParams> GetTechParamsInfoByTime(DateTime start, DateTime end);

        bool InsertTechParamsInfo(TechParams tech);

        bool DeleteTechParamsInfo(TechParams tech);

        bool UpdateTechParams(TechParams tech, int id);
    }
}