# Common Watch — Rain / Ember battle-arena

## Wat dit pakket is
Een presentatiepakket met **19 PNG-bestanden voor runtime**, bronafbeeldingen,
metadata, een implementatieprompt en reproduceerbare offline compositiechecks.
Het verandert geen repository en bevat geen speelbare Godot-demo.

De beeldtaal volgt de gekozen donkere arena: natte verbrande grond, rode lucht,
brandende dorpen, zwarte bomen en cyaan magie. Er zijn **geen hexen, keep of torens
in de middenplaten ingebakken**. Dat maakt de art bruikbaar voor verschillende
bordbreedtes en voor één tot zes aaneengesloten sectoren.

## Begin hier
1. Geef je lokale coding agent `IMPLEMENTATIEPROMPT.txt`.
2. Laat de agent `metadata/render_contract.json` lezen en de actuele Core controleren.
3. Laat de agent de art in de bestaande renderer integreren en echte spelcaptures maken.

**Belangrijk:** de eerder gevraagde 6×9 is niet de momenteel gecontroleerde
Core-topologie: `SharedArena.cs` gebruikt 7 kolommen × 9 rijen per speler.
Er zijn previews van beide; de integratie moet de echte snapshot volgen.
Deze constatering is geen verzoek om de spelregels te veranderen.

## Runtime-onderdelen
| Map | Aantal | Formaat | Toepassing |
|---|---:|---|---|
| assets/ground | 3 | 1024×1024 RGB | Herhaalbare natte grond, gedeelde randen |
| assets/backdrops | 3 | 1024×384 RGBA | Brandende bos-/dorpsrand, zachte onderzijde |
| assets/edges | 3 | 1024×320 / 352×1280 RGBA | Voorgrondstrip en twee buitenkanten |
| assets/structures | 2 | 512×512 RGBA | Keep en verdedigingstoren |
| assets/fx | 5 | 512² / 256² RGBA | Regen, vonken, rook en twee gloedmaskers |
| assets/assembled | 3 | 1024×1536 RGB | Alternatieve samengestelde middenplaten |

Gebruik standaard de losse grond/achtergrond/rand-componenten. De samengestelde
platen zijn een alternatief voor een vaste compositie, niet nóg een laag.
Ze bevatten geen UI, raster, selecties, speleenheden of werkende gebouwen.

## Eenvoudige opbouw
```
                   gedeelde bovenstrook / brandend dorp
  linker rand  |  midden A  |  midden B  |  midden C ...  |  rechter rand
                   grond + runtime hexraster
                   structure- en unitsprites
                   optionele regen/rook/vonken
                   lage decoratie onderaan, HUD erboven
```
Er zijn precies twee eindkappen voor het HELE veld. Een eindkap is geen extra
speelsector. Plaats ze buiten de echte cellen en schilder de basis eronder door,
anders wordt de transparante binnenrand een donkere goot.

## Aansluiten en perspectief
Alle grondvarianten hebben exact dezelfde randkolommen én randrijen. Alle
achtergrondvarianten delen dezelfde horizontale aansluitranden. Hetzelfde geldt
voor de samengestelde platen. Die eigenschappen zijn numeriek gecontroleerd.
Dit is geen garantie voor iedere GPU/schaalinstelling: controleer lineaire
filtering, zoom en fractionele coördinaten in Godot op eventuele haarlijnen.

De middelste illustraties hebben een brede rustige grondzone zonder zijdelingse
muren. Houd hexen exact op de bestaande globale projectie; laat de getekende
horizon nooit de grootte van gameplay-cellen bepalen. Bij 7 kolommen is de
horizontale herhaalafstand 7×sqrt(3)×R. Een sector-bounding-box is door de
versprongen rijen breder dan die herhaalafstand. Die extra halve cel mag je niet
na ieder segment opnieuw toevoegen.

De plaat zelf is getekend. Rechtopstaande gebouwen, bomen en torens mogen niet
nog eens platgedrukt worden wanneer grondcompressie verandert. In layered mode
kan de grondhoogte met de echte geometrie meegaan en blijven de banden en sprites
proportioneel. Zie het contract voor de precieze scheiding.

## Gebouwen
De sprites zijn uit de goedgekeurde laatste conceptplaat gehaald en voorzien van
alpha. Het geregistreerde grondpunt is de voetafdruk, niet de laagste losse
schaduw/bladpixel. `metadata/assets.json` bevat het canvasanker en broncoördinaten.
Pas grootte aan in wereldruimte, niet opnieuw na iedere camerazoom.

Er is één keep-ontwerp en één torenontwerp. Er zijn geen aparte katapult-,
ruïne- of beschadigingsframes. Behoud bestaande art/fallback en de bestaande
schade-/verdwijnanimatie. Rode en blauwe geschilderde vaandels zijn decoratie;
laat eigendom blijken uit de echte game-indicatoren.

## Varianten, transparantie en beperkingen
- De drie middens zijn afgeleid van één nieuwe schildering met faseverschuivingen
  en gedeelde randstroken; niet drie volledig andere dorpen.
- De rechter eindkap is een spiegeling van de linker. De dieren in die randen
  zijn statisch onderdeel van de illustratie, niet geanimeerde gameplay-units.
- Fijne cyan/vuurgloed in de gebouwen is conservatief behouden. Controleer de
  uitsneden op de echte donkere achtergrond; dit zijn geen vectorlagen/rigs.
- FX-bestanden zijn optionele textures. Ze bewegen pas wanneer de game ze laat
  scrollen; ze zijn geen spritesheet-animaties of weersimulatie.
- Er zijn geen fonts, nieuwe spelregels, shader-binaries of uitvoerbare gamebestanden.

## Offline controleren
Alleen Pillow is nodig om de bestaande resultaten te controleren en samen te stellen:
```
python3 tools/validate_pack.py
python3 tools/render_preview.py --segments 3 --columns 7
python3 tools/render_preview.py --segments 6 --columns 7
python3 tools/render_preview.py --segments 1 --columns 6
```
Gebruik de bestaande gepinde art-omgeving van de repository; upgrade geen pakketten
alleen voor deze previews. De optionele authoring-rebuild heeft extra libraries
nodig en hoort NIET bij de runtime of de verplichte game-build.

## Wat daadwerkelijk is getest
PNG-decodering, afmetingen, alpha van uitsneden, bron/output-hashes, aansluitranden
voor alle 3×3 combinaties, 1..6 sector-composities en beide 6/7-kolomvoorbeelden.
De previews zijn met Pillow gemaakt. **Godot, netwerk, gameplay en GPU-rendering
zijn niet uitgevoerd.** De agent moet die integratiestap nog doen.

Bekijk `previews/`, `metadata/validation_report.json` en `docs/BRONNEN.md`.
