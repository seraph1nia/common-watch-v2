# Implementatieprompt

```text
Implementeer de modulaire battle-arena in de lokale Common Watch-repository met:
~/Downloads/common_watch_modular_battle_arena.zip

Lees AGENTS.md, README_NL.md en metadata/render_contract.json. Pak de juiste ZIP veilig uit, bewaar lokale wijzigingen en kopieer de meegeleverde art naar repository-lokale paden. Implementeer de wijzigingen; lever niet alleen een plan.

BELANGRIJK: de oorspronkelijke beeldwens was 6×9, maar de gecontroleerde SharedArena.cs gebruikt momenteel 7×9 per sector. Controleer de actuele code en behoud ALLE autoritatieve cellen. De afbeeldingen hebben bewust geen hexraster en ondersteunen beide. Wijzig Core niet om het plaatje te laten passen.

RENDERING
- Bouw één doorlopend slagveld met 1–6 horizontale sectoren uit de echte Arena-snapshot. Gebruik de grond-, achtergrond- en voorgrondstroken uit het pakket; de drie varianten hebben gedeelde aansluitranden.
- Plaats alleen een linker- en rechtereindstuk aan de BUITENKANT van het complete veld, nooit tussen spelers. Laat basisgrond onder de transparante randen doorlopen. Geen muren of loze stroken op sectornaden.
- Teken hexen en highlights apart uit de bestaande projectie. Voor het huidige pointy-top raster is de sectorafstand W*sqrt(3)*R; neem niet de bounding-boxbreedte inclusief rijverspringing. Gebruik globale rijpariteit, ook over naden.
- Houd ground-compressie, camera, units, structures, effecten en picking in hetzelfde coördinatenstelsel. Geen verdwijnpunt per sector en geen extra verticale vervorming van rechtopstaande sprites.
- assets/assembled bevat optionele samengestelde platen, geen extra laag. Gebruik die niet bovenop de losse componenten.

GEBOUWEN EN EFFECTEN
- Gebruik keep.png en defense_tower.png als losse sprites op de echte structure.Cell-posities. Hun grondankers staan in de metadata. Houd bestaande Watchpost/CatapultTower-art of geometrische fallback waar geen specifiek ontwerp is meegeleverd.
- Sorteer gebouwen én units samen op grondpositie. Behoud actieve/vernietigde toestand, schadefeedback, levensbalken, inspectie en autoritatieve animatietiming.
- Regen, rook, vonken en gloed zijn optionele visuele overlays. Houd ze subtiel, buiten belangrijke informatie, zonder invloed op gameplay. Geen verplicht nieuw particle-systeem; stilstaande overlays zijn een geldige eerste stap.

INTEGRATIE
- Hergebruik BattlefieldView, AssetLoader, WorldArt en HexProjection. Pas asset-ID-validatie/catalogus alleen gericht aan; dit pakketmanifest vervangt assets/catalog.json NIET. Bewaar bronnen en hashes zodat bestaande art-taken niets terugdraaien of verwijderen.
- Deel en cache textures; maak geen gigantische nieuwe texture van zes arena’s en laad niet tegelijk alle losse en samengestelde varianten. Render alleen wat zichtbaar is.
- Behoud het settlement, HUD, multiplayer, routeplanning en debug-fallback. Start dichtbij de eigen sector, met pan/zoom en een aparte overzichtsactie voor het hele veld.

VERIFICATIE
Test bestaande build/core/shared/session/animation-checks en relevante battle-captures. Controleer 1–6 sectoren, aansluitnaden, beweging/picking over grenzen, structuurschade en camera op 1280×720 en 1920×1080. Gebruik legale scenario’s voor spelcaptures; offline pack-previews tellen niet als Godot-verificatie. Lever echte screenshots, gewijzigde bestanden en eerlijke testresultaten. Niet committen of pushen.
```
