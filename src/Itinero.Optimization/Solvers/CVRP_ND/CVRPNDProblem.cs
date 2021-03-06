﻿/*
 *  Licensed to SharpSoftware under one or more contributor
 *  license agreements. See the NOTICE file distributed with this work for 
 *  additional information regarding copyright ownership.
 * 
 *  SharpSoftware licenses this file to you under the Apache License, 
 *  Version 2.0 (the "License"); you may not use this file except in 
 *  compliance with the License. You may obtain a copy of the License at
 * 
 *       http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.LocalGeo;
using Itinero.Optimization.Solvers.CVRP_ND.TourSeeded;
using Itinero.Optimization.Solvers.Shared.NearestNeighbours;

namespace Itinero.Optimization.Solvers.CVRP_ND
{
    /// <summary>
    /// Represents a capacitated VRP.
    /// </summary>
    public class CVRPNDProblem
    {
        private readonly float[][] _travelWeights;
        private readonly float[] _visitWeights;
        private readonly Coordinate[] _visitLocations;
        private readonly HashSet<int> _visits;
        private readonly float _maxWeight;
        private readonly Lazy<NearestNeighbourCache> _nearestNeighbourCacheLazy;
        private readonly Lazy<SeededTourPool> _seededTourPool;

        /// <summary>
        /// Creates a new problem.
        /// </summary>
        /// <param name="travelWeights">The weights between the visits.</param>
        /// <param name="visitWeights">The weights at each visit, if any.</param>
        /// <param name="maxWeight">The maximum weight per vehicle, if any.</param>
        /// <param name="capacityConstraints">The visit costs per metric for each visit and a max per vehicle.</param>
        /// <param name="visits">The required visits, all visits required if null.</param>
        /// <param name="visitLocations">The visit locations if available.</param>
        public CVRPNDProblem(float[][] travelWeights, float[] visitWeights = null, float maxWeight = float.MaxValue, 
            IEnumerable<(string metric, float max, float[] costs)> capacityConstraints = null, IEnumerable<int> visits = null,
            Coordinate[] visitLocations = null)
        {
            _travelWeights = travelWeights;
            _visitWeights = visitWeights;
            _maxWeight = maxWeight;
            _visitLocations = visitLocations;
            CapacityConstraints = capacityConstraints?.ToArray() ?? new (string metric, float max, float[] costs)[0];

            if (visits != null)
            {
                if (visits is HashSet<int> visitsSet)
                {
                    _visits = visitsSet;
                }
                else
                {
                    _visits = new HashSet<int>(visits);
                }
            }
            
            _nearestNeighbourCacheLazy = new Lazy<NearestNeighbourCache>(() =>
                new NearestNeighbourCache(_travelWeights.Length, (x, y) => _travelWeights[x][y]));
            _seededTourPool = new Lazy<SeededTourPool>(() =>
            {
                var pool = new SeededTourPool(this, 100);
                pool.Build();
                return pool;
            });
        }

        /// <summary>
        /// Gets the maximum weight.
        /// </summary>
        public float MaxWeight => _maxWeight;

        /// <summary>
        /// Gets the travel weight between the two given visits.
        /// </summary>
        /// <param name="from">The first visit.</param>
        /// <param name="to">The second visit.</param>
        /// <returns>The travel weight from 'from' to 'to'.</returns>
        public float TravelWeight(int from, int to)
        {
            //System.Diagnostics.Debug.Assert(from >= 0 && from < _travelWeights.Length && to >= 0 && to < _travelWeights.Length);
            return _travelWeights[from][to];
        }

        /// <summary>
        /// Gets the visit weight of the given visit.
        /// </summary>
        /// <param name="visit">The visit.</param>
        /// <returns></returns>
        public float VisitWeight(int visit)
        {
            if (_visitWeights == null) return 0;
            
            return _visitWeights[visit];
        }

        /// <summary>
        /// Gets the location of the given visit.
        /// </summary>
        /// <param name="visit">The visit.</param>
        /// <returns></returns>
        public Coordinate? VisitLocation(int visit)
        {
            return _visitLocations?[visit];
        }
        
        /// <summary>
        /// Returns true if the visit is part of this problem.
        /// </summary>
        /// <param name="visit">The visit.</param>
        /// <returns>True if this visit is part of this problem.</returns>
        public bool Contains(int visit)
        {
            if (_visits != null)
            {
                return _visits.Contains(visit);
            }
            return visit >= 0 && visit < _travelWeights.Length;
        }

        /// <summary>
        /// Gets the capacity constraints.
        /// </summary>
        public (string metric, float max, float[] costs)[] CapacityConstraints { get; }

        /// <summary>
        /// Gets the nearest neighbour cache.
        /// </summary>
        internal NearestNeighbourCache NearestNeighbourCache => _nearestNeighbourCacheLazy.Value;

        /// <summary>
        /// Gets the seeded tour pool.
        /// </summary>
        internal SeededTourPool SeededTourPool => _seededTourPool.Value;

        /// <summary>
        /// Gets the visits.
        /// </summary>
        public IEnumerable<int> Visits => _visits ?? Enumerable.Range(0, _travelWeights.Length);

        /// <summary>
        /// Gets the highest visit id.
        /// </summary>
        public int MaxVisit => _travelWeights.Length;

        /// <summary>
        /// Gets the number of visits.
        /// </summary>
        public int Count
        {
            get
            {
                var count = _travelWeights.Length;
                if (_visits != null) count = _visits.Count;
                return count;
            }
        }
    }
}