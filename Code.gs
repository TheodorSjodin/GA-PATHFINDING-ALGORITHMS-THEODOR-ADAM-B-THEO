/**
 * Pathfinding-benchmark -> Google Sheets
 * Version 2026-10-26-v4
 *
 * Installation:
 *  1. Klistra in hela filen i Apps Script (ersätt gammal kod).
 *  2. Projektinställningar -> Skriptegenskaper -> lägg till  SHARED_SECRET = <ny hemlighet>
 *     (samma värde som i sändarprogrammet).
 *  3. Distribuera -> Hantera distributioner -> penna -> Version: "Ny version" -> Distribuera.
 *     (Webbappen kör ALLTID den distribuerade versionen, inte koden du sparat!)
 *  4. Öppna webbappens /exec-URL i webbläsaren: du ska se {"ok":true,"version":"2026-10-26-v4"}.
 */

const SHEET_NAME = "Results";
const LOG_SHEET_NAME = "Log";
const DIAG_SHEET_NAME = "Diagnos";
const DASHBOARD_NAME = "Dashboard";
const CODE_VERSION = "2026-10-27-v5";
const USE_LOG_SCALE = true; // logaritmisk Y-axel på tids- och längddiagram

const HEADERS = ["Timestamp", "Algorithm", "Width", "Height", "ObstaclePercentage", "SearchTimeMs", "PathLength", "PathFound",
  "PathOverhead", "PeakFrontier", "StoredNodes"];
const LEGACY_COLUMNS = 8; // äldre sändare skickar bara A-H
const ALGORITHMS = ["BFS", "DFS", "Dijkstra", "AStar", "GreedyBestFirst"];
const ALGO_COLORS = ["#0072B2", "#D55E00", "#009E73", "#CC79A7", "#E69F00"];
const NUMERIC_COLS = [2, 3, 4, 5, 6, 8, 9, 10]; // 0-baserat index
const OPTIONAL_NUMERIC_COLS = [6, 8, 9, 10]; // får vara tomma (t.ex. överskott när BFS saknar väg)
// "@" = ren text. Sätts FÖRE skrivning så att Sheets aldrig tolkar texten.
const COL_FORMATS = ["@", "@", "0", "0", "0.00", "0.000", "0", "@", "0.000", "0", "0"];
const ELAPSED_FORMAT = '[h]"|"mm"|"ss';

/* ============================== Webbapp ============================== */

function doGet() {
  return jsonResponse_({ ok: true, version: CODE_VERSION });
}

function doPost(e) {
  let body = "";
  try {
    body = (e && e.postData && e.postData.contents) || "";
    if (!body) throw new Error("No request data received.");

    const data = JSON.parse(body);
    if (!data || data.secret !== getSecret_()) {
      return jsonResponse_({ success: false, error: "Unauthorized", version: CODE_VERSION });
    }
    if (!Array.isArray(data.rows) || data.rows.length === 0) {
      throw new Error("No rows received");
    }

    // Validera och konvertera ALLT innan något skrivs (allt eller inget).
    const rows = data.rows.map(normalizeRow_);

    const lock = LockService.getScriptLock();
    lock.waitLock(30000);
    try {
      const sheet = getResultsSheet_();
      const firstRow = appendRows_(sheet, rows);
      rememberPayload_(body);
      return jsonResponse_({ success: true, received: rows.length, firstRow: firstRow, version: CODE_VERSION });
    } finally {
      lock.releaseLock();
    }
  } catch (err) {
    logEvent_("ERROR", String(err), body);
    rememberPayload_(body);
    return jsonResponse_({ success: false, error: String(err), version: CODE_VERSION });
  }
}

function getSecret_() {
  const s = PropertiesService.getScriptProperties().getProperty("SHARED_SECRET");
  if (!s) throw new Error("SHARED_SECRET saknas i Skriptegenskaper.");
  return s;
}

function jsonResponse_(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}

/* ===================== Parsning och validering ===================== */

/**
 * Gör om ett värde till ett riktigt JS-tal. Accepterar tal, "10", "10.00", "10,00".
 * Avvisar allt annat (t.ex. "26.13.00") i stället för att gissa.
 */
function parseDecimal_(value, allowEmpty, label) {
  if (typeof value === "number") {
    if (!isFinite(value)) throw new Error("Non-finite number for " + label);
    return value;
  }
  if (value === null || value === undefined || (typeof value === "string" && value.trim() === "")) {
    if (allowEmpty) return "";
    throw new Error("Missing numeric value for " + label);
  }
  if (typeof value === "string") {
    const s = value.replace(/[\s ]/g, "");
    if (/^-?\d+$/.test(s)) return Number(s);
    if (/^-?\d+[.,]\d+$/.test(s)) return Number(s.replace(",", "."));
  }
  throw new Error("Invalid numeric value for " + label + ": " + JSON.stringify(value));
}

function normalizeRow_(row, i) {
  if (!Array.isArray(row) || (row.length !== HEADERS.length && row.length !== LEGACY_COLUMNS)) {
    throw new Error("Row " + (i + 1) + " must contain " + LEGACY_COLUMNS + " or " + HEADERS.length + " values.");
  }
  const full = row.length === HEADERS.length ? row : row.concat(["", "", ""]);
  return full.map(function (v, idx) {
    if (NUMERIC_COLS.indexOf(idx) !== -1) {
      return parseDecimal_(v, OPTIONAL_NUMERIC_COLS.indexOf(idx) !== -1, "row " + (i + 1) + ", " + HEADERS[idx]);
    }
    if (typeof v === "boolean") return v ? "Yes" : "No";
    return v === null || v === undefined ? "" : String(v);
  });
}

/* ============================= Skrivning ============================= */

function getResultsSheet_() {
  const sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(SHEET_NAME);
  if (!sheet) throw new Error("Sheet not found: " + SHEET_NAME);
  return sheet;
}

function appendRows_(sheet, rows) {
  if (sheet.getLastRow() === 0) {
    sheet.getRange(1, 1, 1, HEADERS.length).setNumberFormat("@").setValues([HEADERS]);
  } else {
    ensureHeaders_(sheet);
  }
  const first = sheet.getLastRow() + 1;
  const n = rows.length;
  const need = first + n - 1;
  if (need > sheet.getMaxRows()) {
    sheet.insertRowsAfter(sheet.getMaxRows(), need - sheet.getMaxRows());
  }
  const range = sheet.getRange(first, 1, n, HEADERS.length);
  range.setNumberFormats(rows.map(function () { return COL_FORMATS; })); // format FÖRST
  range.setValues(rows);                                                 // sedan riktiga tal
  SpreadsheetApp.flush();
  verifyRows_(sheet, first, rows);
  return first;
}

/** Lägger till rubriker för nya kolumner (I-K) i ett befintligt ark. */
function ensureHeaders_(sheet) {
  if (sheet.getMaxColumns() < HEADERS.length) {
    sheet.insertColumnsAfter(sheet.getMaxColumns(), HEADERS.length - sheet.getMaxColumns());
  }
  const hdr = sheet.getRange(1, 1, 1, HEADERS.length).getValues()[0];
  for (let i = 0; i < HEADERS.length; i++) {
    if (!hdr[i]) sheet.getRange(1, i + 1).setNumberFormat("@").setValue(HEADERS[i]);
  }
}

/** Läser tillbaka det som skrevs. Om något inte är exakt samma tal raderas raderna och anropet misslyckas. */
function verifyRows_(sheet, first, rows) {
  const back = sheet.getRange(first, 1, rows.length, HEADERS.length).getValues();
  for (let r = 0; r < rows.length; r++) {
    for (let k = 0; k < NUMERIC_COLS.length; k++) {
      const c = NUMERIC_COLS[k];
      const want = rows[r][c];
      const got = back[r][c];
      const ok = want === "" ? got === "" : (typeof got === "number" && Math.abs(got - want) < 1e-9);
      if (!ok) {
        sheet.deleteRows(first, rows.length);
        throw new Error("Readback mismatch (" + HEADERS[c] + ", batch row " + (r + 1) + "): wrote " +
          JSON.stringify(want) + " but sheet holds " + kindOf_(got) + " " + String(got) +
          ". Rows were removed again.");
      }
    }
  }
}

/* ============================== Loggning ============================== */

function redact_(text) {
  return String(text).replace(/"secret"\s*:\s*"[^"]*"/g, '"secret":"***"');
}

function rememberPayload_(body) {
  try {
    PropertiesService.getScriptProperties().setProperty("LAST_PAYLOAD", redact_(body).slice(0, 8000));
  } catch (e) { /* diagnostik får aldrig stoppa skrivningen */ }
}

function logEvent_(level, message, body) {
  try {
    const ss = SpreadsheetApp.getActiveSpreadsheet();
    const sh = ss.getSheetByName(LOG_SHEET_NAME) || ss.insertSheet(LOG_SHEET_NAME);
    if (sh.getLastRow() === 0) sh.getRange(1, 1, 1, 4).setNumberFormat("@").setValues([["Tid", "Nivå", "Meddelande", "Payload (utan hemlighet)"]]);
    if (sh.getLastRow() > 600) sh.deleteRows(2, 200);
    const when = Utilities.formatDate(new Date(), ss.getSpreadsheetTimeZone(), "yyyy-MM-dd HH:mm:ss");
    sh.getRange(sh.getLastRow() + 1, 1, 1, 4).setNumberFormat("@")
      .setValues([[when, level, message, redact_(body || "").slice(0, 2000)]]);
  } catch (e) { /* ignorera */ }
}

/** Visar exakt vilka typer och värden som senaste anropet innehöll. */
function describePayload_(body) {
  const data = JSON.parse(body);
  const lines = ["Antal rader: " + (Array.isArray(data.rows) ? data.rows.length : "?")];
  (data.rows || []).slice(0, 5).forEach(function (row, i) {
    lines.push("Rad " + (i + 1) + ": " + row.map(function (v, k) {
      return HEADERS[k] + "=" + JSON.stringify(v) + " (" + typeof v + ")";
    }).join(", "));
  });
  return lines.join("\n");
}

function showLastPayload() {
  const body = PropertiesService.getScriptProperties().getProperty("LAST_PAYLOAD");
  const ui = SpreadsheetApp.getUi();
  if (!body) { ui.alert("Inget anrop har registrerats ännu. Kör testprogrammet en gång."); return; }
  let text;
  try { text = describePayload_(body); } catch (e) { text = "Kunde inte tolka som JSON:\n" + body.slice(0, 1500); }
  ui.alert("Senaste payload (så som Apps Script tog emot den)", text, ui.ButtonSet.OK);
}

/* ============================ Diagnostik ============================ */

function kindOf_(v) {
  if (typeof v === "number") return "tal";
  if (v instanceof Date) return "tid/datum";
  if (v === "" || v === null || v === undefined) return "tom";
  if (typeof v === "string") return "text";
  return typeof v;
}

function fmtCounts_(obj) {
  return Object.keys(obj).map(function (k) { return k + " × " + obj[k]; }).join(", ");
}

function writeTable_(name, rows) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sh = ss.getSheetByName(name) || ss.insertSheet(name);
  sh.clear();
  const width = Math.max.apply(null, rows.map(function (r) { return r.length; }));
  const padded = rows.map(function (r) { const c = r.slice(); while (c.length < width) c.push(""); return c; });
  sh.getRange(1, 1, padded.length, width).setNumberFormat("@").setValues(padded);
  sh.autoResizeColumns(1, width);
  sh.getRange(1, 1, 1, width).setFontWeight("bold");
  return sh;
}

/** Steg 1: Hur ser cellerna FAKTISKT ut inne i arket? */
function diagnoseResults() {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = getResultsSheet_();
  const n = sheet.getLastRow() - 1;
  const out = [["Kontroll", "Resultat"],
    ["Kodversion", CODE_VERSION],
    ["Arkets språk (locale)", ss.getSpreadsheetLocale()],
    ["Arkets tidszon", ss.getSpreadsheetTimeZone()],
    ["Datarader", String(Math.max(n, 0))]];

  if (n >= 1) {
    const range = sheet.getRange(2, 1, n, HEADERS.length);
    const values = range.getValues();
    const display = range.getDisplayValues();
    const formats = range.getNumberFormats();
    NUMERIC_COLS.forEach(function (c) {
      const kinds = {}, fm = {}, shown = {}, examples = [];
      values.forEach(function (row, i) {
        const k = kindOf_(row[c]);
        kinds[k] = (kinds[k] || 0) + 1;
        fm[formats[i][c]] = (fm[formats[i][c]] || 0) + 1;
        shown[display[i][c]] = 1;
        if (k !== "tal" && k !== "tom" && examples.length < 3) {
          examples.push(String.fromCharCode(65 + c) + (i + 2) + " visas \"" + display[i][c] + "\"");
        }
      });
      out.push([HEADERS[c], "celltyper: " + fmtCounts_(kinds)]);
      out.push(["", "talformat: " + fmtCounts_(fm)]);
      if (examples.length) out.push(["", "exempel på celler som INTE är tal: " + examples.join("; ")]);
      if (c === 4) out.push(["", "distinkta visade värden: " + Object.keys(shown).slice(0, 12).join(" | ")]);
    });
    out.push(["Tolkning", "Allt utom 'tal' i Width/Height/Obstacle/SearchTime/PathLength betyder att Sheets har tolkat texten (t.ex. 10.00 som klockslag 10:00)."]);
  }
  writeTable_(DIAG_SHEET_NAME, out);
  ss.toast("Se fliken " + DIAG_SHEET_NAME, "Diagnos klar", 8);
}

/* ============================== Reparation ============================== */

/**
 * Avkodar ETT ObstaclePercentage-värde till det ursprungliga talet.
 * Inga gissade konstanter: tid h:mm tolkas som texten "h.mm" (som sändaren skickade).
 *  - cell tolkad som tid/varaktighet: visas som "h|mm|ss" med ELAPSED_FORMAT, ss måste vara 00
 *  - tal 0 < v < 2 som är exakt k/1440 (hh.mm som serienummer): samma avkodning
 *  - tal >= 2 och text "10.00"/"10,00": redan korrekta
 * Allt annat returneras som status "oklart" och skrivs ALDRIG över.
 */
function decodeObstacle_(value, elapsedDisplay) {
  if (value instanceof Date) {
    const m = /^(\d+)\|(\d\d)\|(\d\d)$/.exec(elapsedDisplay || "");
    if (!m || Number(m[3]) !== 0 || Number(m[2]) > 59) return { status: "oklart", note: "tid " + elapsedDisplay };
    return { status: "ändrad", value: Number(m[1]) + Number(m[2]) / 100, note: "tid " + elapsedDisplay };
  }
  if (typeof value === "number") {
    if (value === 0 || value >= 2) return { status: "oförändrad", value: value };
    const minutes = value * 1440;
    if (value > 0 && Math.abs(minutes - Math.round(minutes)) < 1e-6) {
      const total = Math.round(minutes);
      return { status: "ändrad", value: Math.floor(total / 60) + (total % 60) / 100, note: "serienummer " + value };
    }
    return { status: "oklart", note: "tal " + value };
  }
  if (value === "" || value === null) return { status: "oförändrad", value: "" };
  if (typeof value === "string") {
    try { return { status: "oförändrad", value: parseDecimal_(value, false, "E") }; }
    catch (e) { return { status: "oklart", note: "text " + value }; }
  }
  return { status: "oklart", note: String(value) };
}

function repairDryRun() { repairObstacle_(false); }
function repairApply() { repairObstacle_(true); }

function repairObstacle_(apply) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const sheet = getResultsSheet_();
  const n = sheet.getLastRow() - 1;
  if (n < 1) { ss.toast("Inga data."); return; }
  const rng = sheet.getRange(2, 5, n, 1);
  const values = rng.getValues();
  const origFormats = rng.getNumberFormats();

  const timeRows = [];
  values.forEach(function (r, i) { if (r[0] instanceof Date) timeRows.push(i); });
  const elapsed = {};
  if (timeRows.length) {
    rng.setNumberFormat(ELAPSED_FORMAT);          // tillfälligt: gör serienumret läsbart som h|mm|ss
    const d = rng.getDisplayValues();
    timeRows.forEach(function (i) { elapsed[i] = d[i][0]; });
    rng.setNumberFormats(origFormats);             // återställ
  }

  const counts = { "ändrad": 0, "oförändrad": 0, "oklart": 0 };
  const unclear = [];
  const result = values.map(function (r, i) {
    const d = decodeObstacle_(r[0], elapsed[i]);
    counts[d.status]++;
    if (d.status === "oklart" && unclear.length < 10) unclear.push("E" + (i + 2) + ": " + d.note);
    return [d.status === "oklart" ? r[0] : d.value];
  });
  const distinct = {};
  result.forEach(function (r) { distinct[r[0]] = (distinct[r[0]] || 0) + 1; });

  const report = [["Reparation av ObstaclePercentage", apply ? "UTFÖRD" : "TORRKÖRNING (inget ändrat)"],
    ["Ändras", String(counts["ändrad"])], ["Redan korrekta", String(counts["oförändrad"])],
    ["Oklara (rörs inte)", String(counts["oklart"]) + (unclear.length ? " – " + unclear.join("; ") : "")],
    ["Värden efter reparation", fmtCounts_(distinct)]];

  if (apply) {
    if (counts["oklart"] > 0) {
      report[0][1] = "AVBRUTEN – oklara celler finns, ingenting skrevs";
    } else {
      const backup = backupSheet_(sheet);
      rng.setNumberFormat("0.00");
      rng.setValues(result);
      report.push(["Backup", backup]);
    }
  }
  writeTable_(DIAG_SHEET_NAME, report);
  ss.toast("Se fliken " + DIAG_SHEET_NAME, "Klart", 8);
}

function backupSheet_(sheet) {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const name = sheet.getName() + "_backup_" + Utilities.formatDate(new Date(), ss.getSpreadsheetTimeZone(), "yyyyMMdd_HHmmss");
  sheet.copyTo(ss).setName(name);
  return name;
}

/**
 * SearchTimeMs kan inte repareras säkert ur arket: efter tolkningen som klockslag är
 * originaldecimalerna inte längre åtskiljbara från äkta värden. Säkraste vägen är att
 * arkivera gamla Results (backup) och köra om benchmarken med den nya koden.
 */
function archiveAndReset() {
  const ui = SpreadsheetApp.getUi();
  const answer = ui.alert("Arkivera och nollställ Results",
    "En kopia av Results sparas som ny flik, därefter töms alla datarader och rätt talformat sätts. Fortsätta?",
    ui.ButtonSet.YES_NO);
  if (answer !== ui.Button.YES) return;
  const sheet = getResultsSheet_();
  const backup = backupSheet_(sheet);
  if (sheet.getLastRow() > 1) sheet.deleteRows(2, sheet.getLastRow() - 1);
  const rows = sheet.getMaxRows() - 1;
  if (rows > 0) {
    sheet.getRange(2, 1, rows, HEADERS.length).clearContent()
      .setNumberFormats(Array.from({ length: rows }, function () { return COL_FORMATS; }));
  }
  SpreadsheetApp.getActiveSpreadsheet().toast("Backup: " + backup, "Nollställd", 10);
}

/* ============================== Dashboard ============================== */

function uniqueSorted_(arr) {
  return Array.from(new Set(arr)).sort(function (a, b) { return a - b; });
}

function sizeLabel_(s) { return s + "x" + s; }
function obstacleLabel_(o) { return o + "%"; }

/** Bygger formelsträngar (A1-notation, engelska funktionsnamn – gäller oavsett arkets språk). */
function avgFormula_(valueCol, byRow, algoHdrCell, labelCell, fixedCell) {
  const R = "'" + SHEET_NAME + "'!";
  const rowFilter = byRow === "size"
    ? R + "$C$2:$C,VALUE(LEFT(" + labelCell + ",FIND(\"x\"," + labelCell + ")-1)),"
      + R + "$E$2:$E," + fixedCell
    : R + "$E$2:$E,VALUE(SUBSTITUTE(" + labelCell + ",\"%\",\"\")),"
      + R + "$C$2:$C," + fixedCell;
  return "=IFERROR(AVERAGEIFS(" + R + "$" + valueCol + "$2:$" + valueCol + "," + R + "$B$2:$B," + algoHdrCell + "," + rowFilter + "),NA())";
}

function successFormula_(algoHdrCell, labelCell, fixedCell) {
  const R = "'" + SHEET_NAME + "'!";
  const crit = R + "$B$2:$B," + algoHdrCell + "," + R + "$E$2:$E,VALUE(SUBSTITUTE(" + labelCell + ",\"%\",\"\"))," + R + "$C$2:$C," + fixedCell;
  return "=IFERROR(COUNTIFS(" + crit + "," + R + "$H$2:$H,\"Yes\")/COUNTIFS(" + crit + "),NA())";
}

/**
 * setFormula kräver i vissa språkinställningar (t.ex. svenska) ";" som argumentavgränsare.
 * Testar vilken som fungerar i just det här arket. Formlerna byggs med "," och översätts sedan
 * (inga formler här innehåller kommatecken i textsträngar eller decimaltal).
 */
function detectArgSeparator_(sheet) {
  const cell = sheet.getRange(1, sheet.getMaxColumns());
  const seps = [",", ";"];
  for (let i = 0; i < seps.length; i++) {
    cell.setFormula("=MAX(1" + seps[i] + "2)");
    SpreadsheetApp.flush();
    if (cell.getValue() === 2) { cell.clear(); return seps[i]; }
  }
  cell.clear();
  throw new Error("Kunde inte avgöra vilken formelsyntax arket använder.");
}

function toLocaleFormula_(formula, sep) {
  return sep === "," ? formula : formula.replace(/,/g, sep);
}

function buildDashboard() {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  const res = getResultsSheet_();
  const n = res.getLastRow() - 1;
  if (n < 1) throw new Error("Results är tom.");
  const wh = res.getRange(2, 3, n, 3).getValues(); // C, D, E
  const bad = wh.filter(function (r) { return typeof r[0] !== "number" || typeof r[2] !== "number"; }).length;
  if (bad) throw new Error(bad + " rader har icke-numerisk Width/ObstaclePercentage. Kör Diagnostisera och Reparera först.");

  const sizes = uniqueSorted_(wh.map(function (r) { return r[0]; }));
  const obstacles = uniqueSorted_(wh.map(function (r) { return r[2]; }));
  const midSize = sizes[Math.floor(sizes.length / 2)];
  const midObstacle = obstacles[0];

  const d = ss.getSheetByName(DASHBOARD_NAME) || ss.insertSheet(DASHBOARD_NAME);
  d.getCharts().forEach(function (c) { d.removeChart(c); });
  d.clear();
  d.getRange(1, 1, d.getMaxRows(), d.getMaxColumns()).clearDataValidations();

  d.getRange("A1").setValue("Pathfinding – algoritmjämförelse").setFontSize(16).setFontWeight("bold");
  d.getRange("A2").setValue("Hinderprocent (storleksdiagram):");
  d.getRange("B2").setNumberFormat("0").setValue(midObstacle)
    .setDataValidation(SpreadsheetApp.newDataValidation().requireValueInList(obstacles.map(String), true).build())
    .setBackground("#fff2cc");
  d.getRange("D2").setValue("Kartstorlek (hinderdiagram):");
  d.getRange("E2").setNumberFormat("0").setValue(midSize)
    .setDataValidation(SpreadsheetApp.newDataValidation().requireValueInList(sizes.map(String), true).build())
    .setBackground("#fff2cc");
  const sep = detectArgSeparator_(d);
  const L = function (f) { return toLocaleFormula_(f, sep); };
  d.getRange("A3").setValue("Antal körningar:");
  d.getRange("B3").setFormula(L("=COUNTA('" + SHEET_NAME + "'!$B$2:$B)"));

  const blockH = Math.max(19, Math.max(sizes.length, obstacles.length) + 5);
  const blocks = [
    { title: "Söktid (ms) mot kartstorlek", by: "size", col: "F", type: Charts.ChartType.LINE, h: "Kartstorlek", v: "ms (medel)", log: USE_LOG_SCALE },
    { title: "Söktid (ms) mot hinderprocent", by: "obstacle", col: "F", type: Charts.ChartType.LINE, h: "Hinderprocent", v: "ms (medel)", log: USE_LOG_SCALE },
    { title: "Sökvägslängd mot kartstorlek", by: "size", col: "G", type: Charts.ChartType.LINE, h: "Kartstorlek", v: "Steg (medel)", log: USE_LOG_SCALE },
    { title: "Sökvägslängd mot hinderprocent", by: "obstacle", col: "G", type: Charts.ChartType.LINE, h: "Hinderprocent", v: "Steg (medel)", log: USE_LOG_SCALE },
    { title: "Andel hittade vägar mot hinderprocent", by: "success", col: "H", type: Charts.ChartType.COLUMN, h: "Hinderprocent", v: "Andel", log: false },
    { title: "Sökvägsöverskott (längd / BFS-längd) mot kartstorlek", by: "size", col: "I", type: Charts.ChartType.LINE, h: "Kartstorlek", v: "Överskott (1 = kortast)", log: USE_LOG_SCALE },
    { title: "Sökvägsöverskott (längd / BFS-längd) mot hinderprocent", by: "obstacle", col: "I", type: Charts.ChartType.LINE, h: "Hinderprocent", v: "Överskott (1 = kortast)", log: USE_LOG_SCALE },
    { title: "Största frontier (poster) mot kartstorlek", by: "size", col: "J", type: Charts.ChartType.LINE, h: "Kartstorlek", v: "Poster (medel)", log: USE_LOG_SCALE },
    { title: "Lagrade noder (poster) mot kartstorlek", by: "size", col: "K", type: Charts.ChartType.LINE, h: "Kartstorlek", v: "Poster (medel)", log: USE_LOG_SCALE }
  ];

  blocks.forEach(function (b, k) {
    const r0 = 5 + k * blockH;
    const labels = b.by === "size" ? sizes.map(sizeLabel_) : obstacles.map(obstacleLabel_);
    const fixedCell = b.by === "size" ? "$B$2" : "$E$2";
    d.getRange(r0, 1).setValue(b.title + (b.by === "size" ? " (hinder = vald i B2)" : " (storlek = vald i E2)")).setFontWeight("bold");
    d.getRange(r0 + 1, 1, 1, 1 + ALGORITHMS.length).setNumberFormat("@").setValues([[b.h].concat(ALGORITHMS)]).setFontWeight("bold").setBackground("#f3f3f3");
    d.getRange(r0 + 2, 1, labels.length, 1).setNumberFormat("@").setValues(labels.map(function (l) { return [l]; }));
    const formulas = labels.map(function (_, i) {
      return ALGORITHMS.map(function (_a, j) {
        const hdr = String.fromCharCode(66 + j) + "$" + (r0 + 1);
        const lab = "$A" + (r0 + 2 + i);
        return b.by === "success" ? successFormula_(hdr, lab, "$E$2") : avgFormula_(b.col, b.by, hdr, lab, fixedCell);
      });
    });
    const body = d.getRange(r0 + 2, 2, labels.length, ALGORITHMS.length);
    body.setFormulas(formulas.map(function (row) { return row.map(L); }))
      .setNumberFormat(b.by === "success" ? "0%" : (b.col === "F" || b.col === "I" ? "0.000" : "0"));
    SpreadsheetApp.flush();
    const shown = body.getDisplayValues();
    for (let i = 0; i < shown.length; i++) {
      for (let j = 0; j < shown[i].length; j++) {
        if (shown[i][j] === "#ERROR!") {
          throw new Error("Formelfel i " + body.getCell(i + 1, j + 1).getA1Notation() + ": " +
            body.getCell(i + 1, j + 1).getFormula());
        }
      }
    }

    let chart = d.newChart().setChartType(b.type)
      .addRange(d.getRange(r0 + 1, 1, labels.length + 1, 1 + ALGORITHMS.length))
      .setNumHeaders(1)
      .setPosition(r0, 8, 0, 0)
      .setOption("title", b.title)
      .setOption("legend.position", "bottom")
      .setOption("hAxis.title", b.h)
      .setOption("vAxis.title", b.v)
      .setOption("colors", ALGO_COLORS)
      .setOption("width", 620)
      .setOption("height", 330);
    if (b.type === Charts.ChartType.LINE) chart = chart.setOption("pointSize", 5);
    if (b.log) chart = chart.setOption("vAxis.logScale", true);
    d.insertChart(chart.build());
  });

  d.setColumnWidth(1, 220);
  d.activate();
  ss.toast("Ändra de gula cellerna B2/E2 för att byta urval.", "Dashboard klar", 8);
}

/* ================================ Meny ================================ */

function onOpen() {
  SpreadsheetApp.getUi().createMenu("Pathfinding")
    .addItem("1. Diagnostisera data", "diagnoseResults")
    .addItem("2. Visa senaste payload", "showLastPayload")
    .addSeparator()
    .addItem("3a. Reparera hinderprocent (torrkörning)", "repairDryRun")
    .addItem("3b. Reparera hinderprocent (utför)", "repairApply")
    .addItem("4. Arkivera och nollställ Results", "archiveAndReset")
    .addSeparator()
    .addItem("5. Bygg dashboard", "buildDashboard")
    .addToUi();
}
