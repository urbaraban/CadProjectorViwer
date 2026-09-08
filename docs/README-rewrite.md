# 2Cut rewrite

Active solution: [2Cut.sln](../2Cut.sln) · Branch: `rewrite/2cut` · Spec: [TZ-LaserProjection.md](TZ-LaserProjection.md)

```bash
dotnet run --project src/CadProjector.App
```

## Done

| Sprint | Content |
|--------|---------|
| 0 | Solution skeleton, Avalonia full-canvas |
| 1 | DXF/SVG, canvas, mask, `.cproj`, Virtual Play |
| 2 | Transforms, 4-corner mesh, ILDA, VLT, UDP |
| 3 | Mesh corners UI, Z/DFC, FOV split, better DXF |
| 4 | NxM calibration mesh (per-cell homography), VLT-A + VLT-B, FOV + mesh overlay on canvas |
| 5 | Drag mesh points on canvas, mesh in `.cproj` (schema v2), ALIGN object to plane |
| 6 | Mask drag/resize, `.cdev` profile+mesh, Calibrate Dot/Rect/Grid, layers visibility |
| 7 | Export `.ild`, RU/EN toolbar, Logs panel + UDP smoke script, DXF `IsLayerOn` |
| 8 | Devices hub: per-device mesh + modules, schema v3 `devices`, Play per-device stage |
| 9 | Dynamic N projectors (+/−), dynamic module chain (add/reorder/del), tabbed Devices UX |
| 10 | Реестр модулей: все 19 модулей из legacy MonchaSDK, произвольный порядок и параметры на устройство |
| 11 | Сетка — такой же модуль; модули с геометрией рисуются и правятся на столе, опционально уходят на проектор |
| 12 | Unit-тесты + CI; unsaved warn; ColorMode UI; multi-scene +/− |

**Devices:** list + Add/Remove (не только A/B); вкладки Device / FOV / Mesh / Modules  

**Modules:** цепочка на проектор — упорядоченный список, порядок в списке = порядок выполнения.
Стадий больше нет: калибровочная сетка (`Mesh`) стоит в цепочке как обычный модуль, и всё до неё
работает в координатах сцены, а всё после — в координатах проектора. Один модуль можно добавить
несколько раз, каждый со своими параметрами; сеток на устройстве тоже может быть несколько.

| Группа | Модули |
|--------|--------|
| Оптимизация | Unduplicate, ShortestPath |
| Сервис | BlankBridge, PointMass, ScanRateGradient, ScanRateSplit, BlankCircle, LinesSkipper, LinesGroupSplitter, LineGridSplitter |
| Трансформация | Rotate2D, MoveScale2D, RectProportion, AroundZero, ResolutionMultiplier |
| Корректоры | Mesh, ArctanCorrector, AxisGradient, ZCorrector, DeepFrameCutter |

Default (как в legacy `LProjector`):
Unduplicate → ShortestPath → BlankBridge → PointMass → ScanRateGradient → (DFC off) →
**Mesh** → ScanRateSplit → (Arctan off) → ZCorrector

**Модули с геометрией** реализуют `IRenderableModule`: `GetGeometry()` — контур в нормализованных
координатах устройства, `GetAnchors()` / `MoveAnchor()` — ручки, которые можно тянуть на столе.
Две галочки на модуле управляют его геометрией независимо: «показывать на столе» и «отдавать на
проектор» (во втором случае контур подмешивается в кадр сразу после шага модуля, поэтому его
преобразуют все последующие модули, включая сетку). Сейчас геометрию дают Mesh, DeepFrameCutter,
RectProportion, BlankCircle, LineGridSplitter, AxisGradient и ArctanCorrector — у двух последних
контур только для наблюдения, тянуть в них нечего.

**Как добавить модуль:** класс с `IFrameModule` (или `PointModule`, или `IRenderableModule` — если
есть геометрия) в `CadProjector.Rendering/Modules` + одна строка в `ModuleRegistry`. Параметры —
публичные свойства; каталог, редактор в UI и сериализация строятся по ним автоматически
(`[ModuleParam]` задаёт подпись и шаг).

### История правок

`CadProjector.Core/Editing`: `IEditAction` + `ValueEdit<T>` + `EditHistory`. Действие попадает в
историю уже применённым — жест сам меняет модель, истории нужно лишь знать, как вернуться назад
(`Revert`) и как повторить (`Apply`).

- **Один жест — одна запись.** `Push` объединяет соседние записи с одинаковым merge-ключом
  (`drawable:{id}:move`, `mask:bounds`, `module:{id}`, …), пока между ними меньше 800 мс. Поэтому
  протяжка мышью или серия кликов по спиннеру откатываются одним Ctrl+Z. Отпускание кнопки мыши
  (`SceneCanvas.GestureEnded` → `MainViewModel.EndGesture`) закрывает запись сразу.
- **Пустые правки не пишутся:** `IsNoOp` сравнивает before/after, так что синхронизация панелей с
  моделью не засоряет историю (для этого `ModuleState` реализует `IEquatable`).
- **Что записывается:** перемещение объекта на столе и правка в панели «Трансформ», границы маски,
  протяжка ручек любого `IRenderableModule`, параметры и галочки модулей, узлы и размер сетки,
  добавление / удаление / порядок модулей в цепочке.
- Модульные правки хранятся как снимок `ModuleState` (флаги + `params` + копия сетки), правки
  цепочки — как снимок порядка. Undo/Redo — кнопки ↶ / ↷ на тулбаре, Ctrl+Z и Ctrl+Y
  (Ctrl+Shift+Z). Глубина — 200 записей, load проекта и импорт чертежа историю чистят.

**Pipeline:** FOV split → цепочка модулей устройства по порядку  
**`.cproj`:** `moduleChain[]` на device с `params{}`, флагами `showOnTable` / `projectGeometry` и
`mesh{}` у модуля сетки; схема v3 (стадии + сетка на устройстве), старые flat-поля и `modules`
мигрируют при load
