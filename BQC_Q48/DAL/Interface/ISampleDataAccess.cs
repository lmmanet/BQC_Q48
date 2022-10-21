using BQJX.Common;
using System;
using System.Collections.Generic;

namespace Q_Platform.DAL
{
    public interface ISampleDataAccess
    {
        bool DeleteSampleInfo(SampleInfo sampleInfo);
        List<SampleInfo> GetSampleInfoById(int start, int count);
        List<SampleInfo> GetSampleInfoByName(string name);
        List<SampleInfo> GetSampleInfoBySnNum(string SnNum);
        List<SampleInfo> GetSampleInfoByTime(DateTime start, DateTime end);
        bool InsertSampleInfo(SampleInfo sampleInfo);
    }
}