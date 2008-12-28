﻿using System;
using System.Linq;
using System.Collections.Generic;
using QuickGraph.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Pex.Framework;

namespace QuickGraph.Algorithms.Search
{
    [TestClass]
    public class BreadthFirstAlgorithmSearchTest
    {
        [TestMethod]
        public void BreadthFirstSearchAll()
        {
            foreach(var g in TestGraphFactory.GetAdjacencyGraphs())
                foreach(var v in g.Vertices)
                    RunBfs(g,v);
        }

        [PexMethod]
        public void RunBfs<TVertex, TEdge>(IVertexAndEdgeListGraph<TVertex, TEdge> g, TVertex sourceVertex)
            where TEdge : IEdge<TVertex>
        {
            var parents = new Dictionary<TVertex, TVertex>();
            var distances = new Dictionary<TVertex, int>();
            TVertex currentVertex = default(TVertex);
            int currentDistance = 0;
            var algo = new BreadthFirstSearchAlgorithm<TVertex, TEdge>(g);

            algo.InitializeVertex += (sender, args) =>
            {
                Assert.AreEqual(algo.VertexColors[args.Vertex], GraphColor.White);
            };

            algo.DiscoverVertex += (sender, args) =>
            {
                var u = args.Vertex;
                Assert.AreEqual(algo.VertexColors[u], GraphColor.Gray);
                if (u.Equals(sourceVertex))
                    currentVertex = sourceVertex;
                else
                {
                    Assert.IsNotNull(currentVertex);
                    Assert.AreEqual(parents[u], currentVertex);
                    Assert.AreEqual(distances[u], currentDistance + 1);
                    Assert.AreEqual(distances[u], distances[parents[u]] + 1);
                }
            };
            algo.ExamineEdge += (sender, args) =>
            {
                Assert.AreEqual(args.Edge.Source, currentVertex);
            };

            algo.ExamineVertex += (sender, args) =>
            {
                var u = args.Vertex;
                currentVertex = u;
                // Ensure that the distances monotonically increase.
                Assert.IsTrue(
                       distances[u] == currentDistance
                    || distances[u] == currentDistance + 1
                    );

                if (distances[u] == currentDistance + 1) // new level
                    ++currentDistance;
            };
            algo.TreeEdge += (sender, args) =>
            {
                var u = args.Edge.Source;
                var v = args.Edge.Target;

                Assert.AreEqual(algo.VertexColors[v], GraphColor.White);
                Assert.AreEqual(distances[u], currentDistance);
                parents[v] = u;
                distances[v] = distances[u] + 1;
            };
            algo.NonTreeEdge += (sender, args) =>
            {
                var u = args.Edge.Source;
                var v = args.Edge.Target;

                Assert.IsFalse(algo.VertexColors[v] == GraphColor.White);

                if (algo.VisitedGraph.IsDirected)
                {
                    // cross or back edge
                    Assert.IsTrue(distances[v] <= distances[u] + 1);
                }
                else
                {
                    // cross edge (or going backwards on a tree edge)
                    Assert.IsTrue(
                        distances[v] == distances[u]
                        || distances[v] == distances[u] + 1
                        || distances[v] == distances[u] - 1
                        );
                }
            };

            algo.GrayTarget += (sender, args) =>
            {
                Assert.AreEqual(algo.VertexColors[args.Edge.Target], GraphColor.Gray);
            };
            algo.BlackTarget += (sender, args) =>
            {
                Assert.AreEqual(algo.VertexColors[args.Edge.Target], GraphColor.Black);

                foreach (var e in algo.VisitedGraph.OutEdges(args.Edge.Target))
                    Assert.IsFalse(algo.VertexColors[e.Target] == GraphColor.White);
            };

            algo.FinishVertex += (sender, args) =>
            {
                Assert.AreEqual(algo.VertexColors[args.Vertex], GraphColor.Black);
            };


            parents.Clear();
            distances.Clear();
            currentDistance = 0;

            foreach (var v in g.Vertices)
            {
                distances[v] = int.MaxValue;
                parents[v] = v;
            }
            distances[sourceVertex] = 0;
            algo.Compute(sourceVertex);

            // All white vertices should be unreachable from the source.
            foreach (var v in g.Vertices)
            {
                if (algo.VertexColors[v] == GraphColor.White)
                {
                    //!IsReachable(start,u,g);
                }
            }

            // The shortest path to a child should be one longer than
            // shortest path to the parent.
            foreach (var v in g.Vertices)
            {
                if (!parents[v].Equals(v)) // *ui not the root of the bfs tree
                    Assert.AreEqual(distances[v], distances[parents[v]] + 1);
            }
        }
    }
}
