The following files were copied from
https://github.com/seraph1nia/odot-game at revision
`501a3de7c3f5cf8b5e1726e01dbaefad5ea82f35`, under the upstream GPL-3.0-only
license recorded at the repository root:

- `tests/Game.Core.Tests/ResourceTests.cs`
- `tests/Game.Core.Tests/AuthoritySessionTests.cs`
- `tests/Game.Core.Tests/HexBoardTests.cs`
- `tests/Game.Core.Tests/CampaignFixture.cs`

The local `AuthoritySessionTests.cs` capacity case now expects six players and rejects a seventh; the local `HexBoardTests.cs` geometry expectations now describe the explicitly requested six-column, 54-cell default board. Other copied cases remain unchanged.

They exercise the authoritative Core independently of either client. New
slice-specific authority tests live one directory above.
