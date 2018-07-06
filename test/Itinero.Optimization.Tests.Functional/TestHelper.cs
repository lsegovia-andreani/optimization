﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Optimization.Tests.Functional
{
    public static class TestHelper
    {
        public static void RunWithIntermedidates(this Func<Action<IEnumerable<Result<Route>>>, IEnumerable<Result<Route>>> func, string name)
        {
            var allIintermediateRoutes = new List<Route>();
            var localFunc = new Func<IEnumerable<Result<Route>>>(() => func((intermedidateResults) =>
            {
                var routes = new List<Route>();
                foreach (var result in intermedidateResults)
                {
                    if (result.IsError)
                    {
                        continue;
                    }
                    
                    routes.Add(result.Value);
                }
                routes.Sort();
                routes.AddTimeStamp();
                routes.AddRouteId();
                allIintermediateRoutes.AddRange(routes);
                allIintermediateRoutes.WriteGeoJsonOneFile(name + "-all.geojson");
            }));
            
            RouteExtensions.ResetTimeStamp();
            var results = localFunc.TestPerf(name).ToList();
            results.WriteStats();
            results.WriteGeoJson(name + "-{0}.geojson");
        }
    }
}