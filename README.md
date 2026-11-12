# Pathfinding Algorithm Benchmark

A Unity-based project comparing five pathfinding algorithms under different grid sizes and obstacle densities.

## Overview

The purpose of this project is to investigate and compare the performance of different pathfinding algorithms in a grid-based environment.

The simulation generates a grid containing obstacles and tests how different algorithms find a path from a starting position to a goal.

The project focuses on comparing search time, path length and search efficiency under different conditions.

## Algorithms

Five pathfinding algorithms are implemented and compared:

| Algorithm | Description |
|---|---|
| Breadth-First Search (BFS) | Explores nodes level by level and finds a shortest path in an unweighted grid. |
| Depth-First Search (DFS) | Explores one branch as far as possible before backtracking. |
| Dijkstra's Algorithm | Finds a shortest path using accumulated path costs. |
| A* Search | Combines accumulated path cost with a heuristic estimate of the remaining distance. |
| Greedy Best-First Search | Prioritizes nodes based on their estimated distance to the goal. |

## Heuristic

A* and Greedy Best-First Search use Manhattan distance as their heuristic:

**h(n) = |xₙ - xgoal| + |yₙ - ygoal|**

This heuristic is suitable for the simulation because movement is restricted to four directions: up, down, left and right.

A* combines the actual cost from the starting node with the heuristic:

**f(n) = g(n) + h(n)**

Greedy Best-First Search uses only the heuristic:

**f(n) = h(n)**

## Performance Metrics

The algorithms are evaluated using the following metrics:

- **Search Time (ms):** Time required to find a path.
- **Path Length:** Number of steps between the starting position and the goal.
- **Explored Nodes:** Number of nodes processed during the search.
- **Path Validity:** Checks whether the resulting path follows the grid rules.

Search time, path length and whether a path was found are saved to a CSV file. Explored nodes and path validity are currently reported in the Unity Console.

## Environment

The simulation uses a two-dimensional grid represented in a three-dimensional Unity environment.

The grid contains:
- A starting position.
- A goal position.
- Obstacles that block movement.
- Four-directional movement between adjacent cells.

## Google Sheets Integration

The project includes an optional Google Sheets integration that allows benchmark results to be synchronized from Unity to a Google Spreadsheet.

The integration uses:

GoogleSheetsBatchSync.cs – handles communication between Unity and Google Sheets.

Google Apps Script – receives benchmark data and writes it to a spreadsheet.

A local CSV file – stores benchmark results before synchronization.

Setting Up Your Own Google Spreadsheet

To use the synchronization system with your own spreadsheet:

Create a new Google Spreadsheet.

Open Extensions → Apps Script.

Add the Google Apps Script code provided in this repository.

Deploy the script as a Web App.

Copy the Web App URL.

Open GoogleSheetsBatchSync.cs in the Unity project.

Replace the existing Web App URL with your own URL.

Save the script and run the Unity project.

Important: The Google Apps Script and Unity synchronization code must use the same data format and column structure.

The expected CSV columns are:

Timestamp, Algorithm, Width, Height, ObstaclePercentage, SearchTimeMs, PathLength, PathFound

Using the Synchronization System

Once configured, benchmark results can be synchronized from the local CSV file to your Google Spreadsheet using the synchronization system.

Each user should configure their own spreadsheet and Web App URL before running synchronization.

Note: The Google Sheets integration is optional. The pathfinding algorithms and benchmark can be used independently of Google Sheets synchronization.

### Option 2: Running without google sheets integration

1. Clone or download this repository.
2. Open Unity Hub.
3. Select **Add project from disk**.
4. Select the downloaded project folder.
5. Open the project using the appropriate Unity version.

## Technologies

- Unity
- C#
- Git and GitHub
- Google Sheets
- CSV data logging

## Project Authors

Theodor Sjödin, Adam Berglund, Theo Arvidsson

