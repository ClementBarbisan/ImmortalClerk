using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

/// <summary>
/// Implementation of Amit Patel's A* Pathfinding algorithm studies
/// https://www.redblobgames.com/pathfinding/a-star/introduction.html
/// </summary>
public static class AStar
{
    /// <summary>
    /// Returns the best path as a List of Squares
    /// </summary>
    public static List<Square> Search(Square start, Square goal)
    {
        Dictionary<Square, Square> came_from = new Dictionary<Square, Square>();
        Dictionary<Square, float> cost_so_far = new Dictionary<Square, float>();

        List<Square> path = new List<Square>();

        SimplePriorityQueue<Square> frontier = new SimplePriorityQueue<Square>();
        frontier.Enqueue(start, 0);

        came_from.Add(start, start);
        cost_so_far.Add(start, 0);
        Square current = null;
        while (frontier.Count > 0)
        {
            current = frontier.Dequeue();
            if (current == goal) break; // Early exit

            foreach (Square next in current.Sides)
            {
                float new_cost = cost_so_far[current] + 1;
                if (next != null && (!cost_so_far.ContainsKey(next) || new_cost < cost_so_far[next]))
                {
                    cost_so_far[next] = new_cost;
                    came_from[next] = current;
                    float priority = new_cost + Heuristic(next, goal);
                    frontier.Enqueue(next, priority);
                    next.Weight = new_cost;
                }
            }
        }

        while (current != start)
        {
            path.Add(current);
            current = came_from[current];
        }
        path.Reverse();

        return path;
    }

    public static float Heuristic(Square a, Square b)
    {
        return Mathf.Abs(a.Coords.x - b.Coords.x) + Mathf.Abs(a.Coords.y - b.Coords.y);
    }
}