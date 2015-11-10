﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2015 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using NUnit.Framework;
using OsmSharp.Logistics.Routes;
using OsmSharp.Logistics.Routing.TSP;
using OsmSharp.Math.Random;
using OsmSharp.Routing.Osm.Vehicles;
using System;
using System.Linq;
using System.Collections.Generic;
using OsmSharp.Routing.Algorithms;

namespace OsmSharp.Logistics.Tests.Routing.TSP
{
    /// <summary>
    /// Contains tests for the TSP-router.
    /// </summary>
    [TestFixture]
    public class TSPRouterTests
    {
        /// <summary>
        /// Tests the TSP-router for the case when the matrix calculations fail.
        /// </summary>
        [Test]
        public void TestMatrixCalculationNotSucceeded()
        {
            var weightMatrixAlgorithm = new WeightMatrixAlgorithmMock()
            {
                HasSucceeded = false
            };

            // build router.
            var router = new RouterMock();
            var tspSolver = new TSPSolverMock(new Route(new int[] { 0, 1 }), 10);
            var tspRouter = new TSPRouter(router, Vehicle.Car.Fastest(),
                new Math.Geo.GeoCoordinate[] { 
                    new Math.Geo.GeoCoordinate(0, 0),
                    new Math.Geo.GeoCoordinate(1, 1) }, 0, 0, tspSolver, weightMatrixAlgorithm);
            // run.
            tspRouter.Run();

            Assert.AreEqual(false, tspRouter.HasSucceeded);
            Assert.IsNotNull(tspRouter.ErrorMessage);
        }

        /// <summary>
        /// Tests the TSP-router for the case when the matrix calculations for the first location.
        /// </summary>
        [Test]
        public void TestMatrixCalculationNotSucceededForFirst()
        {
            var errors = new Dictionary<int, LocationError>();
            errors.Add(0, new LocationError()
                {
                    Code = LocationErrorCode.Unknown,
                    Message = "What happened?"
                });
            var weightMatrixAlgorithm = new WeightMatrixAlgorithmMock()
            {
                HasSucceeded = true,
                Errors = errors
            };

            // build router.
            var router = new RouterMock();
            var tspSolver = new TSPSolverMock(new Route(new int[] { 0, 1 }), 10);
            var tspRouter = new TSPRouter(router, Vehicle.Car.Fastest(),
                new Math.Geo.GeoCoordinate[] { 
                    new Math.Geo.GeoCoordinate(0, 0),
                    new Math.Geo.GeoCoordinate(1, 1) }, 0, 0, tspSolver, weightMatrixAlgorithm);
            // run.
            tspRouter.Run();

            Assert.AreEqual(false, tspRouter.HasSucceeded);
            Assert.IsNotNull(tspRouter.ErrorMessage);
        }

        /// <summary>
        /// Tests the TSP-router for the case when the matrix calculations for the last location.
        /// </summary>
        [Test]
        public void TestMatrixCalculationNotSucceededForLast()
        {
            var errors = new Dictionary<int, LocationError>();
            errors.Add(1, new LocationError()
            {
                Code = LocationErrorCode.Unknown,
                Message = "What happened?"
            });
            var weightMatrixAlgorithm = new WeightMatrixAlgorithmMock(
                new Tuple<int, int>[] {
                    new Tuple<int, int>(0, 0)
                })
            {
                HasSucceeded = true,
                Errors = errors,
                Weights = new float[][] { new float[] { 0 } }
            };

            // build router.
            var router = new RouterMock();
            var tspSolver = new TSPSolverMock(new Route(new int[] { 0, 1 }), 10);
            var tspRouter = new TSPRouter(router, Vehicle.Car.Fastest(),
                new Math.Geo.GeoCoordinate[] { 
                    new Math.Geo.GeoCoordinate(0, 0),
                    new Math.Geo.GeoCoordinate(1, 1) }, 0, 1, tspSolver, weightMatrixAlgorithm);
            // run.
            tspRouter.Run();

            Assert.AreEqual(false, tspRouter.HasSucceeded);
            Assert.IsNotNull(tspRouter.ErrorMessage);

            // build router.
            tspSolver = new TSPSolverMock(new Route(new int[] { 0 }), 0);
            tspRouter = new TSPRouter(router, Vehicle.Car.Fastest(),
                new Math.Geo.GeoCoordinate[] { 
                    new Math.Geo.GeoCoordinate(0, 0),
                    new Math.Geo.GeoCoordinate(1, 1) }, 0, 0, tspSolver, weightMatrixAlgorithm);
            // run.
            tspRouter.Run();

            Assert.AreEqual(true, tspRouter.HasSucceeded);
            Assert.IsNull(tspRouter.ErrorMessage);
        }

        /// <summary>
        /// Tests a simple TSP.
        /// </summary>
        [Test]
        public void TestTwoPointsClosed()
        {
            StaticRandomGenerator.Set(4541247);

            // build test case.
            var router = new RouterMock();
            var tspSolver = new TSPSolverMock(new Route(new int[] { 0, 1 }), 10);
            var tspRouter = new TSPRouter(router, Vehicle.Car.Fastest(),
                new Math.Geo.GeoCoordinate[] { 
                    new Math.Geo.GeoCoordinate(0, 0),
                    new Math.Geo.GeoCoordinate(1, 1) }, 0, 0, tspSolver);

            // run.
            tspRouter.Run();

            Assert.IsNotNull(tspRouter);
            Assert.IsTrue(tspRouter.HasRun);
            Assert.IsTrue(tspRouter.HasSucceeded);
            var route = tspRouter.BuildRoute();
            Assert.AreEqual(3, route.Segments.Count);
            Assert.AreEqual(0, route.Segments[0].Latitude);
            Assert.AreEqual(0, route.Segments[0].Longitude);
            Assert.AreEqual(1, route.Segments[1].Latitude);
            Assert.AreEqual(1, route.Segments[1].Longitude);
            Assert.AreEqual(0, route.Segments[0].Latitude);
            Assert.AreEqual(0, route.Segments[0].Longitude);
            var routes = tspRouter.BuildRoutes();
            Assert.AreEqual(2, routes.Count);
            Assert.AreEqual(0, routes[0].Segments[0].Latitude);
            Assert.AreEqual(0, routes[0].Segments[0].Longitude);
            Assert.AreEqual(1, routes[0].Segments[1].Latitude);
            Assert.AreEqual(1, routes[0].Segments[1].Longitude);
            Assert.AreEqual(1, routes[1].Segments[0].Latitude);
            Assert.AreEqual(1, routes[1].Segments[0].Longitude);
            Assert.AreEqual(0, routes[1].Segments[1].Latitude);
            Assert.AreEqual(0, routes[1].Segments[1].Longitude);
            var rawRoute = tspRouter.RawRoute;
            Assert.AreEqual(new int[] { 0, 1 }, rawRoute.ToArray());
        }

        /// <summary>
        /// Tests a simple TSP.
        /// </summary>
        [Test]
        public void TestTwoPointsOpen()
        {
            StaticRandomGenerator.Set(4541247);

            // build test case.
            var router = new RouterMock();
            var tspSolver = new TSPSolverMock(new Route(new int[] { 0, 1 }, null), 10);
            var tspRouter = new TSPRouter(router, Vehicle.Car.Fastest(),
                new Math.Geo.GeoCoordinate[] { 
                    new Math.Geo.GeoCoordinate(0, 0),
                    new Math.Geo.GeoCoordinate(1, 1) }, 0, null, tspSolver);

            // run.
            tspRouter.Run();

            Assert.IsNotNull(tspRouter);
            Assert.IsTrue(tspRouter.HasRun);
            Assert.IsTrue(tspRouter.HasSucceeded);
            var route = tspRouter.BuildRoute();
            Assert.AreEqual(2, route.Segments.Count);
            Assert.AreEqual(0, route.Segments[0].Latitude);
            Assert.AreEqual(0, route.Segments[0].Longitude);
            Assert.AreEqual(1, route.Segments[1].Latitude);
            Assert.AreEqual(1, route.Segments[1].Longitude);
            var routes = tspRouter.BuildRoutes();
            Assert.AreEqual(1, routes.Count);
            Assert.AreEqual(0, routes[0].Segments[0].Latitude);
            Assert.AreEqual(0, routes[0].Segments[0].Longitude);
            Assert.AreEqual(1, routes[0].Segments[1].Latitude);
            Assert.AreEqual(1, routes[0].Segments[1].Longitude);
            var rawRoute = tspRouter.RawRoute;
            Assert.AreEqual(new int[] { 0, 1 }, rawRoute.ToArray());
        }

        /// <summary>
        /// Tests a simple TSP.
        /// </summary>
        [Test]
        public void TestTwoPointsFixed()
        {
            StaticRandomGenerator.Set(4541247);

            // build test case.
            var router = new RouterMock();
            var tspSolver = new TSPSolverMock(new Route(new int[] { 0, 1 }, 1), 10);
            var tspRouter = new TSPRouter(router, Vehicle.Car.Fastest(),
                new Math.Geo.GeoCoordinate[] { 
                    new Math.Geo.GeoCoordinate(0, 0),
                    new Math.Geo.GeoCoordinate(1, 1) }, 0, 1, tspSolver);

            // run.
            tspRouter.Run();

            Assert.IsNotNull(tspRouter);
            Assert.IsTrue(tspRouter.HasRun);
            Assert.IsTrue(tspRouter.HasSucceeded);
            var route = tspRouter.BuildRoute();
            Assert.AreEqual(2, route.Segments.Count);
            Assert.AreEqual(0, route.Segments[0].Latitude);
            Assert.AreEqual(0, route.Segments[0].Longitude);
            Assert.AreEqual(1, route.Segments[1].Latitude);
            Assert.AreEqual(1, route.Segments[1].Longitude);
            var routes = tspRouter.BuildRoutes();
            Assert.AreEqual(1, routes.Count);
            Assert.AreEqual(0, routes[0].Segments[0].Latitude);
            Assert.AreEqual(0, routes[0].Segments[0].Longitude);
            Assert.AreEqual(1, routes[0].Segments[1].Latitude);
            Assert.AreEqual(1, routes[0].Segments[1].Longitude);
            var rawRoute = tspRouter.RawRoute;
            Assert.AreEqual(new int[] { 0, 1 }, rawRoute.ToArray());
        }
    }
}