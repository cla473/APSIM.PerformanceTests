﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APSIM.PerformanceTests.Portal.Models
{
    public class PredictedObservedValue
    {
        public int ID { get; set; }
        public int PredictedObservedDetailsID { get; set; }
        public int SimulationsID { get; set; }
        public string MatchName { get; set; }
        public string MatchValue { get; set; }
        public string MatchName2 { get; set; }
        public string MatchValue2 { get; set; }
        public string MatchName3 { get; set; }
        public string MatchValue3 { get; set; }
        public string ValueName { get; set; }
        public Nullable<double> PredictedValue { get; set; }
        public Nullable<double> ObservedValue { get; set; }

        public virtual PredictedObservedDetail PredictedObservedDetail { get; set; }
        public virtual Simulation Simulation { get; set; }
    }
}