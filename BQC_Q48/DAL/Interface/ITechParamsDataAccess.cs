using BQJX.Common;
using System;
using System.Collections.Generic;

namespace Q_Platform.DAL
{
    public interface ITechParamsDataAccess
    {
        List<TechParamsInfo> FindTechParamsById(int id);

        List<TechParamsInfo> FindTechParamsByName(string techName);

        List<TechParamsInfo> GetTechParamsInfoById(int start, int end);

        List<TechParamsInfo> GetTechParamsInfoByTechName(string name);

        List<TechParamsInfo> GetTechParamsInfoByTime(DateTime start, DateTime end);

        bool InsertTechParamsInfo(TechParamsInfo tech);

        bool DeleteTechParamsInfo(TechParamsInfo tech);

        bool UpdateTechParams(TechParamsInfo tech, int id);
    }
}