# Pathfinding Algorithm Benchmark

A Unity-based benchmark comparing five pathfinding algorithms — BFS, DFS, Dijkstra's, A* and Greedy Best-First Search — on grids of different sizes and obstacle densities. The project measures search time, path length, path overhead compared to the shortest path, and memory use (frontier size and stored nodes). Results can optionally be sent automatically to Google Sheets, where an Apps Script builds charts that compare the algorithms.

## Table of Contents

- [Overview](#overview)
- [Algorithms](#algorithms)
- [Heuristic](#heuristic)
- [Performance Metrics](#performance-metrics)
- [Environment](#environment)
- [Running the Project](#running-the-project)
  - [Running a Benchmark in a Build](#running-a-benchmark-in-a-build)
- [Google Sheets Integration](#google-sheets-integration)
  - [Setting Up Your Own Spreadsheet](#setting-up-your-own-spreadsheet)
  - [Synchronization](#synchronization)
  - [Spreadsheet Features](#spreadsheet-features)
  - [Expected Data Format](#expected-data-format)
  - [Important Notes](#important-notes)
- [Project Structure](#project-structure)
- [Technologies](#technologies)
- [Project Authors](#project-authors)

## Overview

The purpose of this project is to investigate and compare the performance of different pathfinding algorithms in a grid-based environment.

The simulation generates a grid containing obstacles and tests how different algorithms find a path from a starting position to a goal. All five algorithms are run on the same generated map, so the results for one map can be compared directly.

The project focuses on comparing search time, path length, how close each path is to the shortest possible path, and memory use under different conditions.

## Algorithms

Five pathfinding algorithms are implemented and compared:

| Algorithm | Description |
|---|---|
| Breadth-First Search (BFS) | Explores nodes level by level and finds a shortest path in an unweighted grid. |
| Depth-First Search (DFS) | Explores one branch as far as possible before backtracking. Finds a path if one exists, but not necessarily the shortest one. |
| Dijkstra's Algorithm | Finds a shortest path using accumulated path costs. |
| A* Search | Combines accumulated path cost with a heuristic estimate of the remaining distance. |
| Greedy Best-First Search | Prioritizes nodes based on their estimated distance to the goal. Fast, but the path is not guaranteed to be the shortest. |

In this environment every step costs the same, so BFS, Dijkstra's Algorithm and A* find a shortest path. DFS and Greedy Best-First Search find a path, but it can be longer than the shortest one.

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

| Metric | Description | Saved to CSV |
|---|---|---|
| **Search Time (ms)** | Time required to find a path. | Yes (`SearchTimeMs`) |
| **Path Length** | Number of steps between the starting position and the goal. | Yes (`PathLength`) |
| **Path Found** | Whether the algorithm found a path. | Yes (`PathFound`) |
| **Path Overhead** | Path length divided by the BFS path length on the same map. 1.0 means the path is as short as the shortest path; 33.0 means it is 33 times longer. Empty if BFS found no path. | Yes (`PathOverhead`) |
| **Peak Frontier** | Largest size of the algorithm's queue, stack or priority queue during the search. | Yes (`PeakFrontier`) |
| **Stored Nodes** | Total number of entries in the algorithm's data structures (for example the parent map, cost map and visited set). This counts entries, not bytes, so it is a relative measure of memory use. | Yes (`StoredNodes`) |
| **Explored Nodes** | Number of nodes processed during the search. | No (Unity Console only) |
| **Path Validity** | Checks whether the resulting path follows the grid rules. | No (Unity Console only) |

Path overhead uses the BFS result of the same run as its reference, so BFS must run first for each map (it is first in the algorithm list).

## Environment

The simulation uses a two-dimensional grid represented in a three-dimensional Unity environment.

The grid contains:
- A starting position.
- A goal position.
- Obstacles that block movement.
- Four-directional movement between adjacent cells.

Each generated map contains a guaranteed winding path between the start and the goal, and obstacles are placed up to the configured obstacle percentage.

### Benchmark Configuration Used in the Study

The results reported with this project were produced with:

- Square grids with sizes 250, 500, 1000, 2500 and 5000 cells per side.
- Obstacle percentages 10 %, 20 %, 30 % and 40 %.
- 500 repetitions (maps) for each combination, with all five algorithms run on each map. This gives 10 000 maps and 50 000 results.

## Running the Project

The project can be run without configuring the Google Sheets integration.

1. Clone or download this repository.
2. Open Unity Hub.
3. Select **Add project from disk**.
4. Select the downloaded project folder.
5. Open the project using Unity 6.5 (6000.5.9f1), or the version listed under [Technologies](#technologies).
6. Open the `Main` scene and press **Play**.

If **Run On Start** (in the *Benchmark* section of the `BeanBagPathfinding` component) is enabled, the five algorithms run once on the current grid when the scene starts. Results are appended to `PathfindingResults.csv` in Unity's persistent data folder.

Google Sheets integration is optional and can be configured separately using the instructions below.

### Running a Benchmark in a Build

Long benchmarks run faster in a build than in the Unity Editor, because the Editor adds overhead.

1. Open **File → Build Profiles**, choose your platform and make sure **Development Build** is **not** ticked.
2. Make sure the `Main` scene is first in the scene list.
3. Build, then start the program. On Windows, from the build folder:

   ```
   <YourGame>.exe -batchmode -nographics -logFile benchmark.log
   ```

   `-batchmode -nographics` starts the program without a window. Output from `Debug.Log` is written to `benchmark.log` in the build folder.
4. Results are written to `PathfindingResults.csv` in the persistent data folder (on Windows `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\`).

Tips for reliable measurements:

- Run all measurements for one dataset in the same way (same build type, same computer, other programs closed).
- The first map after the program starts is slower because of warm-up. Exclude it from timing analysis.
- Running several instances at the same time affects `SearchTimeMs`. Path length, path overhead, frontier size and stored nodes are not affected.
- The program does not quit by itself after the last map unless you add `Application.Quit()` to your runner.
- When you start a new dataset, delete both `PathfindingResults.csv` and `PathfindingResults.syncedRows`.

## Google Sheets Integration

The project includes an optional Google Sheets integration for collecting and analyzing benchmark results.

The integration consists of two components:

- **GoogleSheetsBatchSync.cs:** Unity script responsible for sending local CSV results to Google Sheets.
- **Google Apps Script:** Receives, validates and stores the submitted results in a spreadsheet.

### Setting Up Your Own Spreadsheet

Each user can connect the project to their own Google Sheets spreadsheet.

#### Step 1 – Create a Google Sheets Spreadsheet

1. Create a new spreadsheet using Google Sheets.
2. Rename the spreadsheet to whatever you prefer.
3. Rename the first tab to `Results`.
4. Open **Extensions → Apps Script**. Create the Apps Script project this way, from your own spreadsheet, so that the script is bound to that spreadsheet.

#### Step 2 – Install the Apps Script

1. Open the `/Code.gs` file in this repository.
2. Copy its contents.
3. Paste the code into the Apps Script editor, replacing the default code.
4. Save the project.

The script expects a spreadsheet tab named `Results`. It will create the `Log`, `Diagnos` and `Dashboard` tabs when needed.

#### Step 3 – Configure the Shared Secret

The Unity application and Google Apps Script must use the same shared secret.

In Apps Script:

1. Open **Project Settings**.
2. Find **Script Properties**.
3. Add a property:
   - Property: `SHARED_SECRET`
   - Value: Your chosen secret string.

Choose a unique value for your own setup. Do not commit your secret to a public repository.

#### Step 4 – Deploy the Web App

1. Select **Deploy → New deployment**.
2. Choose **Web app** as the deployment type.
3. Set the application to execute as your Google account.
4. Configure access according to your intended users and Google's available access options.
5. Deploy the application.
6. Copy the generated Web App URL ending in `/exec`.
7. Open the URL in a browser. You should see a short message containing the script version, for example `{"ok":true,"version":"..."}`. This confirms that the deployment is running the code you pasted.

Keep the deployment URL private to your intended users where possible.

A web app always runs the **deployed** version of the code, not the version you saved. After you change the script, choose **Deploy → Manage deployments**, edit the deployment and select **New version**.

#### Step 5 – Configure Unity

1. Open the Unity project.
2. Find the GameObject containing `GoogleSheetsBatchSync`.
3. Locate the `Google Sheets` section in the Inspector.
4. Paste your Web App URL into `Web App Url`.
5. Enter the same shared secret used in Apps Script under `Shared Secret`.

The integration is now configured to send benchmark results to your spreadsheet.

### Synchronization

The Unity application:

- Reads benchmark results from `PathfindingResults.csv`.
- Sends results in batches.
- Uses a local `PathfindingResults.syncedRows` file to track previously synchronized rows.
- Retains local CSV results if a synchronization request fails.

The default batch size is 50 rows, with an automatic synchronization interval of 60 seconds. For large benchmarks the batch size can be raised (for example to 200) to reduce the number of requests.

If you switch to a different spreadsheet, reset the local `PathfindingResults.syncedRows` file so that previously synchronized rows can be uploaded to the new destination. Previously uploaded rows will then be sent again, so use an empty spreadsheet to avoid duplicate records.

`PathfindingResults.syncedRows` contains the number of data rows that have already been uploaded. Keep it consistent with the CSV file:

- Do not edit or delete rows in `PathfindingResults.csv` while the program is running.
- If you delete the CSV file, delete `PathfindingResults.syncedRows` too.
- If you remove rows from the CSV file by hand, set the counter to the number of data rows that remain and have already been uploaded.
- The CSV file must end with a line break, and every row must have 8 or 11 fields. Otherwise synchronization stops with `Invalid CSV row`.

### Spreadsheet Features

The Apps Script includes additional tools available through the custom **Pathfinding** menu:

- Diagnose data types and spreadsheet formatting.
- Inspect the latest received payload.
- Diagnose and repair obstacle percentage values.
- Archive and reset the Results sheet.
- Generate a dashboard with algorithm comparisons.

The dashboard compares the algorithms by map size and obstacle percentage: search time, path length, path overhead, peak frontier, stored nodes and the share of runs that found a path. The yellow cells at the top select which obstacle percentage and map size are shown. The charts update when new rows arrive. Run **Build dashboard** again if a new map size or obstacle percentage is added.

### Expected Data Format

The integration supports two result formats:

**Legacy format (8 columns):**

`Timestamp, Algorithm, Width, Height, ObstaclePercentage, SearchTimeMs, PathLength, PathFound`

**Extended format (11 columns):**

`Timestamp, Algorithm, Width, Height, ObstaclePercentage, SearchTimeMs, PathLength, PathFound, PathOverhead, PeakFrontier, StoredNodes`

Both formats are accepted by the Apps Script. Rows in the legacy format get empty values in the three extra columns, and the missing headers are added to an existing sheet automatically.

Numbers use a dot as the decimal separator (for example `10.00` and `31.396`). The Apps Script converts every numeric field to a real number before writing it and checks the value after writing.

### Important Notes

- The Google Sheets integration is optional.
- Each user should configure their own spreadsheet and deployment.
- The Apps Script must be deployed again as a new version whenever its code is updated.
- The local CSV file remains the source of the results before synchronization.
- The shared secret is included in the Unity client and should not be treated as a secure password. Do not use it to protect sensitive data or rely on it as the only security measure for a publicly accessible Web App.

## Project Structure

```text
GA-PATHFINDING-ALGORITHMS/
├── Assets/
│   ├── Scenes/
│   │   └── Main.unity
│   ├── Scripts/
│   │   ├── BeanBagPathfinding.cs     # the five algorithms and result logging
│   │   ├── GoogleSheetsBatchSync.cs  # uploads CSV results to Google Sheets
│   │   └── ...
│   └── ...
├── Packages/
├── ProjectSettings/
├── Code.gs                           # Google Apps Script for the spreadsheet
└── README.md
```

<!--
## Screenshots

Add screenshots to an `Images` folder, then remove this comment:

### Unity Simulation

![Unity simulation](Images/simulation.png)

### Google Sheets Dashboard

![Google Sheets dashboard](Images/dashboard.png)
-->

## Technologies

- Unity 6.5 (6000.5.9f1)
- C#
- Google Apps Script
- Google Sheets
- CSV data logging
- Git and GitHub

## Project Authors

Theodor Sjödin, Adam Berglund, Theo Arvidsson
