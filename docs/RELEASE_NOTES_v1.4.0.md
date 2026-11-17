## PBE Pro v1.4.0

### New
- **Update notifications:** The Start page now checks GitHub for a newer release and shows an "Update available" banner with a **View release** button. The check runs once per session, fails silently if you're offline, and the banner stays dismissed once you close it.
- **About section in Settings:** Shows the app version, product version, author, and a link to the GitHub repository.

### Changed
- **Scoring is now points-based:** Quiz result percentages and per-category stats are calculated from points earned versus points possible, not from the number of correct answers. Partial credit now counts toward your score. Category stats display as `Points X / Y`.

### Fixed
- **Question editor no longer loses edits:** Question and answer text now saves automatically shortly after you stop typing, and any pending changes are saved when you leave the page or close the app. Edits no longer wait for the field to lose focus, and a question can't disappear from the list while you're editing it.
- **Theme handling:** Fixed a problem where the app could fail to follow Windows' light/dark setting. It's now applied only once the window has loaded, and switching theme modes no longer targets the wrong window.

### Internal
- Version bumped from 1.2.0 to 1.4.0.
- Author and repository URL are now embedded in assembly metadata via the csproj.
- Code formatting cleanup in several files, with no functional changes.
