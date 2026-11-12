using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleSheetsBatchSync : MonoBehaviour
{
    [Header("Google Sheets")]
    public string webAppUrl = "";
    public string sharedSecret = "";

    [Header("Batch Settings")]
    [Min(1)]
    public int batchSize = 50;

    [Min(10)]
    public int syncIntervalSeconds = 60;

    private string csvPath;
    private string cursorPath;
    private bool syncing;

    void Start()
    {
        csvPath = Path.Combine(
            Application.persistentDataPath,
            "PathfindingResults.csv"
        );

        cursorPath = Path.Combine(
            Application.persistentDataPath,
            "PathfindingResults.syncedRows"
        );

        StartCoroutine(SyncLoop());
    }

    IEnumerator SyncLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(syncIntervalSeconds);

            if (!syncing)
            {
                yield return SyncPendingRows();
            }
        }
    }

    public void SyncNow()
    {
        if (!syncing)
        {
            StartCoroutine(SyncPendingRows());
        }
    }

    IEnumerator SyncPendingRows()
    {
        if (syncing)
            yield break;

        if (string.IsNullOrWhiteSpace(webAppUrl) ||
            string.IsNullOrWhiteSpace(sharedSecret))
        {
            Debug.LogWarning(
                "Google Sheets URL or secret is missing."
            );
            yield break;
        }

        if (!File.Exists(csvPath))
            yield break;

        syncing = true;

        try
        {
            string[] lines = File.ReadAllLines(csvPath);

            if (lines.Length <= 1)
                yield break;

            int totalRows = lines.Length - 1;
            int syncedRows = 0;

            if (File.Exists(cursorPath))
            {
                int.TryParse(
                    File.ReadAllText(cursorPath),
                    out syncedRows
                );
            }

            syncedRows = Mathf.Clamp(
                syncedRows, 0, totalRows
            );

            while (syncedRows < totalRows)
            {
                int count = Mathf.Min(
                    batchSize,
                    totalRows - syncedRows
                );

                List<string[]> batch = new List<string[]>();

                for (int i = 0; i < count; i++)
                {
                    string line = lines[1 + syncedRows + i];

                    string[] fields = line.Split(',');

                    // 8 fält = gamla rader, 11 fält = rader med PathOverhead,
                    // PeakFrontier och StoredNodes (Apps Script godtar båda).
                    if (fields.Length != 8 && fields.Length != 11)
                    {
                        Debug.LogError(
                            "Invalid CSV row " +
                            (syncedRows + i + 1) +
                            ". Sync stopped."
                        );
                        yield break;
                    }

                    batch.Add(fields);
                }

                string json = BuildJson(batch);

                using (UnityWebRequest request =
                    new UnityWebRequest(
                        webAppUrl, "POST"))
                {
                    byte[] body = Encoding.UTF8.GetBytes(json);

                    request.uploadHandler =
                        new UploadHandlerRaw(body);

                    request.downloadHandler =
                        new DownloadHandlerBuffer();

                    request.SetRequestHeader(
                        "Content-Type", "application/json"
                    );

                    yield return request.SendWebRequest();

                    string response =
                        request.downloadHandler.text;

                    if (request.result !=
                            UnityWebRequest.Result.Success ||
                        !response.Contains("\"success\":true"))
                    {
                        Debug.LogWarning(
                            "Google Sheets sync failed. " +
                            "Local results are retained. Response: " +
                            response
                        );
                        yield break;
                    }
                }

                syncedRows += count;

                File.WriteAllText(
                    cursorPath,
                    syncedRows.ToString()
                );

                Debug.Log(
                    "Synced " + syncedRows +
                    " of " + totalRows + " results."
                );
            }
        }
        finally
        {
            syncing = false;
        }
    }

    string BuildJson(List<string[]> rows)
    {
        StringBuilder json = new StringBuilder();

        json.Append("{\"secret\":\"");
        json.Append(EscapeJson(sharedSecret));
        json.Append("\",\"rows\":[");

        for (int i = 0; i < rows.Count; i++)
        {
            if (i > 0)
                json.Append(",");

            json.Append("[");

            for (int j = 0; j < rows[i].Length; j++)
            {
                if (j > 0)
                    json.Append(",");

                json.Append("\"");
                json.Append(EscapeJson(rows[i][j]));
                json.Append("\"");
            }

            json.Append("]");
        }

        json.Append("]}");

        return json.ToString();
    }

    string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}