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

using OsmSharp.Logistics.Routes;
using OsmSharp.Logistics.Routes.Cycles;
using OsmSharp.Logistics.Solvers.GA;
using System;
using System.Collections.Generic;

namespace OsmSharp.Logistics.Solutions.TSP.GA.Operators
{
    /// <summary>
    /// An edge assembly crossover.
    /// </summary>
    public class EAXOperator : ICrossOverOperator<ITSP, IRoute>
    {
        private readonly int _maxOffspring;
        private readonly EdgeAssemblyCrossoverSelectionStrategyEnum _strategy;
        private readonly bool _nn;

        /// <summary>
        /// Creates a new EAX crossover.
        /// </summary>
        public EAXOperator()
            : this(30, EAXOperator.EdgeAssemblyCrossoverSelectionStrategyEnum.SingleRandom, true)
        {

        }

        /// <summary>
        /// Creates a new EAX crossover.
        /// </summary>
        public EAXOperator(int maxOffspring,
            EdgeAssemblyCrossoverSelectionStrategyEnum strategy,
            bool nn)
        {
            _maxOffspring = maxOffspring;
            _strategy = strategy;
            _nn = nn;
        }

        /// <summary>
        /// Returns the name of this operator.
        /// </summary>
        public string Name
        {
            get
            {
                if (_strategy == EdgeAssemblyCrossoverSelectionStrategyEnum.SingleRandom)
                {
                    if (_nn)
                    {
                        return string.Format("EAX_(SR{0}_NN)", _maxOffspring);
                    }
                    return string.Format("EAX_(SR{0})", _maxOffspring);
                }
                else
                {
                    if (_nn)
                    {
                        return string.Format("EAX_(MR{0}_NN)", _maxOffspring);
                    }
                    return string.Format("EAX_(MR{0})", _maxOffspring);
                }
            }
        }

        /// <summary>
        /// An enumeration of the crossover types.
        /// </summary>
        public enum EdgeAssemblyCrossoverSelectionStrategyEnum
        {
            /// <summary>
            /// SingleRandom.
            /// </summary>
            SingleRandom, // EAX-1AB
            /// <summary>
            /// MultipleRandom.
            /// </summary>
            MultipleRandom
        }

        #region ICrossOverOperation<int,Problem> Members

        private List<int> SelectCycles(
            List<int> cycles)
        {
            List<int> starts = new List<int>();
            if (_strategy == EdgeAssemblyCrossoverSelectionStrategyEnum.MultipleRandom)
            {
                foreach (int cycle in cycles)
                {
                    if (OsmSharp.Math.Random.StaticRandomGenerator.Get().Generate(1.0) > 0.25)
                    {
                        starts.Add(cycle);
                    }
                }
                return starts;
            }
            else
            {
                if (cycles.Count > 0)
                {
                    int idx = OsmSharp.Math.Random.StaticRandomGenerator.Get().Generate(cycles.Count);
                    starts.Add(cycles[idx]);
                    cycles.RemoveAt(idx);
                }
            }
            return starts;
        }

        #endregion

        /// <summary>
        /// Applies this operator using the given solutions and produces a new solution.
        /// </summary>
        /// <returns></returns>
        public IRoute Apply(ITSP problem, IRoute solution1, IRoute solution2, out double fitness)
        {
            //var originalProblem = problem;
            //var originalSolution1 = solution1;
            //var originalSolution2 = solution2;
            //if (!problem.IsClosed)
            //{ // convert to closed problem.
            //    OsmSharp.Logging.Log.TraceEvent("EAXOperator.Apply", Logging.TraceEventType.Warning,
            //        string.Format("EAX overator cannot be applied to 'open' TSP's: converting problem to a closed equivalent."));

            //    problem = problem.ToClosed();
            //    solution1 = new Route(solution1, true);
            //    solution2 = new Route(solution2, true);
            //}

            if ((solution1.First == solution1.Last) != problem.Last.HasValue) { throw new ArgumentException("Route and problem have to be both closed."); }
            if ((solution2.First == solution2.Last) != problem.Last.HasValue) { throw new ArgumentException("Route and problem have to be both closed."); }
            
            fitness = double.MaxValue;
            var weights = problem.Weights;

            // first create E_a
            var e_a = new AsymmetricCycles(solution1.Count);
            foreach(var edge in solution1.Pairs())
            {
                e_a.AddEdge(edge.From, edge.To);
            }

            // create E_b
            var e_b = new int[solution2.Count];
            foreach(var edge in solution2.Pairs())
            {
                e_b[edge.To] = edge.From;
            }

            // create cycles.
            var cycles = new AsymmetricAlternatingCycles(solution2.Count);
            for (var idx = 0; idx < e_b.Length; idx++)
            {
                var a = e_a[idx];
                if (a != Constants.NOT_SET)
                {
                    var b = e_b[a];
                    if (idx != b && b != Constants.NOT_SET)
                    {
                        cycles.AddEdge(idx, a, b);
                    }
                }
            }

            // the cycles that can be selected.
            var selectableCycles = new List<int>(cycles.Cycles.Keys);

            int generated = 0;
            Route best = null;
            while (generated < _maxOffspring
                && selectableCycles.Count > 0)
            {
                // select some random cycles.
                var cycleStarts = this.SelectCycles(selectableCycles);

                // copy if needed.
                AsymmetricCycles a = null;
                if (_maxOffspring > 1)
                {
                    a = e_a.Clone();
                }
                else
                {
                    a = e_a;
                }

                // take e_a and remove all edges that are in the selected cycles and replace them by the eges
                var nextArrayA = a.NextArray;
                foreach (int start in cycleStarts)
                {
                    var current = start;
                    var currentNext = cycles.Next(current);
                    do
                    {
                        a.AddEdge(currentNext.Value, currentNext.Key);

                        current = currentNext.Value;
                        currentNext = cycles.Next(current);
                    } while (current != start);
                }

                // connect all subtoures.
                var cycleCount = a.Cycles.Count;
                while (cycleCount > 1)
                {
                    // get the smallest tour.
                    var currentTour = new KeyValuePair<int, int>(-1, int.MaxValue);
                    foreach (KeyValuePair<int, int> tour in a.Cycles)
                    {
                        if (tour.Value < currentTour.Value)
                        {
                            currentTour = tour;
                        }
                    }

                    // first try nn approach.
                    var weight = double.MaxValue;
                    var selectedFrom1 = -1;
                    var selectedFrom2 = -1;
                    var selectedTo1 = -1;
                    var selectedTo2 = -1;

                    var ignoreList = new bool[a.Length];
                    int from;
                    int to;
                    from = currentTour.Key;
                    ignoreList[from] = true;
                    to = nextArrayA[from];
                    do
                    {
                        // step to the next ones.
                        from = to;
                        to = nextArrayA[from];

                        //ignore_list.Add(from);
                        ignoreList[from] = true;
                    } while (from != currentTour.Key);

                    if (_nn)
                    { // only try tours containing nn.

                        from = currentTour.Key;
                        to = nextArrayA[from];
                        double weightFromTo = weights[from][to];
                        do
                        {
                            // check the nearest neighbours of from
                            foreach (var nn in problem.GetNNearestNeighbours(10, from))
                            {
                                var nnTo = nextArrayA[nn];

                                if (nnTo != Constants.NOT_SET &&
                                    !ignoreList[nn] &&
                                    !ignoreList[nnTo])
                                {
                                    double mergeWeight =
                                        (weights[from][nnTo] + weights[nn][to]) -
                                        (weightFromTo + weights[nn][nnTo]);
                                    if (weight > mergeWeight)
                                    {
                                        weight = mergeWeight;
                                        selectedFrom1 = from;
                                        selectedFrom2 = nn;
                                        selectedTo1 = to;
                                        selectedTo2 = nnTo;
                                    }
                                }
                            }

                            // step to the next ones.
                            from = to;
                            to = nextArrayA[from];
                        } while (from != currentTour.Key);
                    }
                    if (selectedFrom2 < 0)
                    {
                        // check the nearest neighbours of from
                        foreach (var customer in solution1)
                        {
                            int customerTo = nextArrayA[customer];

                            if (!ignoreList[customer] &&
                                !ignoreList[customerTo])
                            {
                                var mergeWeight =
                                    (weights[from][customerTo] + weights[customer][to]) -
                                    (weights[from][to] + weights[customer][customerTo]);
                                if (weight > mergeWeight)
                                {
                                    weight = mergeWeight;
                                    selectedFrom1 = from;
                                    selectedFrom2 = customer;
                                    selectedTo1 = to;
                                    selectedTo2 = customerTo;
                                }
                            }
                        }
                    }

                    a.AddEdge(selectedFrom1, selectedTo2);
                    a.AddEdge(selectedFrom2, selectedTo1);

                    cycleCount--;
                }

                var newRoute = new Route(new int[] { problem.First }, problem.Last);
                var previous = problem.First;
                var next = nextArrayA[problem.First];
                do
                {
                    newRoute.InsertAfter(previous, next);
                    previous = next;
                    next = nextArrayA[next];
                }
                while (next != Constants.NOT_SET &&
                    next != problem.First);

                var newFitness = 0.0;
                foreach(var edge in newRoute.Pairs())
                {
                    newFitness = newFitness + weights[edge.From][edge.To];
                }

                if (newRoute.Count == solution1.Count)
                {
                    if (best == null ||
                        fitness > newFitness)
                    {
                        best = newRoute;
                        fitness = newFitness;
                    }

                    generated++;
                }
            }

            if (best == null)
            {
                best = new Route(new int[] { problem.First }, problem.Last);
                var previous = problem.First;
                var next = e_a[problem.First];
                do
                {
                    best.InsertAfter(previous, next);
                    previous = next;
                    next = e_a[next];
                }
                while (next != Constants.NOT_SET &&
                    next != problem.First);

                fitness = 0.0;
                foreach (var edge in best.Pairs())
                {
                    fitness = fitness + weights[edge.From][edge.To];
                }
            }

            //if(!originalProblem.IsClosed)
            //{ // the original problem was 'open' so convert the route again.
            //    best = new Route(best, false);
            //}

            return best;
        }
    }
}