# Automation protocol (2Cut)

Hub: multiple listeners (UDP Binary / UDP Text / TCP Text). Commands fan into one handler.

## UDP Binary (legacy MES / PointScenario)

Little-endian. Shared header (16+ bytes):

| Offset | Type | Field |
|--------|------|-------|
| 0 | 8 ASCII | Header (`Points`, `Filename`, `Filepath`), padded with spaces |
| 8 | int32 | TaskId |
| 12 | int16 | TableId |
| 14 | int16 | Command string length |
| 16 | ASCII | Commands joined with `&` (`CLEAR`, `PLAY`, `SHOW`, `OFF`, …) |
| 16+cmdLen | … | Scenario body |

### Points body (MUST)

| Field | Type |
|-------|------|
| nameLen | int16 |
| name | `nameLen` bytes (1 byte = 1 char) |
| pathCount | int16 |
| per path: isClosed | byte (0 / ≠0) |
| pointsCount | int16 |
| points | `pointsCount` × (float64 X, float64 Y) |

Closed paths: if last ≠ first, first point is appended.

### Filename / Filepath body

| Field | Type |
|-------|------|
| strLen | int16 |
| string | `strLen` bytes, encoding Windows-1251 |

`Filename` is resolved against hub WorkFolder; `Filepath` is absolute.

## Text (TCP / UDP Text) — priority for phone & new clients

Line-oriented (`\n` on TCP). Tokens: `Name` or `Name:arg`, separated by `;`.

Examples:

```
PLAY
CLEAR;LOAD:C:\jobs\part.dxf;PLAY
OFF
```

Aliases: `SHOW`≈`PLAY`, `STOP`≈`OFF`, `OPEN`/`FILE`≈`LOAD`.

Reply (TCP/UDP text): `OK` or `ERR:message` (one line).

## JSON (v0, optional)

If a line starts with `{`, it is parsed as JSON:

```json
{"cmd":"play"}
{"cmd":"load","path":"C:\\jobs\\part.dxf"}
{"cmd":"stop"}
{"cmd":"clear"}
```

`command` is accepted as alias of `cmd`; `file` as alias of `path`.
