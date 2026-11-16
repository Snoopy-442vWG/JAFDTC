# JAFDTC 1.5.1 (442 vWG fork by Snoopy) - Change Log since 1.4.0

Airframe: A-10C II. Build date 16-Nov-26. Fork: github.com/Snoopy-442vWG/JAFDTC

## New

### DSMS - dumb bombs and new profile fields
- New munitions in the DSMS munition list: **Mk-82, Mk-82 Air, Mk-84, BDU-33, BDU-50LD, BDU-50PO, BDU-50LGB**.
- New profile fields, shown where the munition's profile has them: **DES TOF, MIN ALT, MIN TOF, HD TOF, LD TOF**. TOF values accept
  decimals from 0.00 to 99.9. **CBU-87** also gets DES TOF (OSB 19) and MIN ALT (OSB 18).
- **Release mode** can be changed on Mk-82 Air, BDU-33 and all three BDU-50 types.
- **GBU-54:** Impact Azimuth (IMP AZ) and Impact Angle (IMP ANG) can be set and are uploaded.
- The Escape Maneuver option is labeled **CLM** (was CLB) to match the jet.

### DSMS - multiple named profiles and the Battle Book
- A munition can have **multiple profiles**: the jet's default profile plus any number of custom, named profiles.
- **Profile** drop-down on the munition page. **New...** creates a profile (name 1-8 characters: A-Z, 0-9, space, period). **Delete**
  removes a custom profile from the configuration.
- Custom profiles are saved in the JAFDTC configuration and are **created on the jet at upload** (profile page > VIEW PRO > name typed
  on the CDU > NEW > settings > SAVE). They start as a copy of the munition's default profile.
- **"Add Battle Book profile..."** drop-down (with a confirmation prompt) adds a profile from the v303 Battle Book and fills in
  DTOF (or HD TOF for Mk-82 Air), MALT, ripple quantity and interval, escape maneuver, fuze, and release mode.
  **75 profiles:** Mk-82 (15), Mk-82 Air (9, "A 10HD2" ...), Mk-84 (6), CBU-87 (6), BDU-33 (15), BDU-50LD (15, named "50 20LD2" ...),
  BDU-50PO (9, named "50A10HD2" ...). Values are as printed on the Battle Book (BDU-50 profiles use the Mk-82 / Mk-82 Air values).
- The **Z-sled picture** for the selected Battle Book profile is shown beside the settings (75 pictures, personal use).
- **Profile order on upload:**
  - The default profiles named MK-82, MK82APO, BDU-33, BDU-50PO, BDU-50LD and BDU-50LGB are moved to the **end** of the DSMS profile
    list (they are not deleted). Nothing is pressed on the jet if the list is already in order.
  - When custom profiles exist the whole list is also sorted: Precision (GBUs, CBU-103, CBU-105), General Purpose, Rockets, Maverick;
    within a munition TOSS, diving high-altitude (HA) then low (DB, LD), steep to shallow, release quantity high to low. A profile
    order set on the DSMS Profile Order tab is used for the munitions instead.
  - New profiles are added at the end of the jet's list, so the sort assumes that.

### Miscellaneous
- **MFCD OSB 12-15** page assignments (COMM, STAT, MAV, CDU, MSG, DSMS, TGP, TAD) for the left and right MFCDs are set on upload. After the
  layout is set, OSB 12 is pressed on the left MFCD and OSB 13 on the right MFCD to return to the pages.
- Page layout is now three columns (CDU/AAP/Autopilot, Altitude/TACAN/IFF, **MFCD Layout:** with its own title), spaced like the other pages.

### Flight plan (new)
- Two new check boxes at the top of the **Waypoints** page (A-10C only):
  - **Use waypoints to create flight plan** (off by default). On upload, after the waypoints are entered on the CDU, JAFDTC presses FPM,
    opens FPBUILD for flight plan 1 and enters every waypoint by its nav point name starting at entry **02**. Entry 01 (INIT POSIT) is
    never touched. The CDU shows three entries per page, so JAFDTC pages with the PG rocker (down) after every three entries. The same
    nav point name may appear more than once in the flight plan.
  - **Set Flight Plan Sequencing Mode to Manual**. On the FPMENU page, before the flight plan is built, JAFDTC presses the first left
    key to switch AUTO to MAN if it is not already MAN. This is the same setting as "FPM Flight Plan 01 Mode" on the Miscellaneous page
    and works with or without the first check box.
- Only flight plan 1 is built. Waypoint names are entered exactly as they were created on the CDU (uppercase, letters/digits/space, up to
  12 characters, must start with a letter).

### TAD
- **Range** list now includes the 442 vWG mod levels: 0.06, 0.12, 0.25, 0.5, 1, 2, 5, 10, 20, 40, 80, 160 (it was 5, 10, 20, 40, 80, 160).
  The jet starts at 0.06 with the mod, so 0.06 is the default and the upload steps the range up from there with the HOTAS DMS switch
  (DMS aft, one click per level). A TAD range saved before this version is not carried over and shows 0.06.
- **Requires the TAD realistic zoom levels mod:** the new range levels only work if the pilot has the TAD realistic zoom levels mod installed in DCS. Without it the jet has only 5, 10, 20, 40, 80 and 160, starts at 5, and the upload will not end on the level chosen in JAFDTC.

### Settings
- **Data folder:** Settings has a Data folder box with Browse... and Default buttons that choose where JAFDTC keeps configurations,
  settings, databases and kneeboards (default `Documents\JAFDTC`). "Copy my existing files" carries files over (old files are left in
  place, existing files at the destination are not replaced). JAFDTC closes and uses the new folder when started again. The choice is
  stored in `%LOCALAPPDATA%\JAFDTC\datafolder.txt`; if that folder cannot be used JAFDTC falls back to the default.
- Dialog rows are sized to their content so no text overlaps.

### Versioning and updates
- Version is **1.5.1 (442 vWG)** in the About box, welcome text and the title bar (the title bar still said 1.4.0 before).
- The update check reads `VERSION.txt` from github.com/Snoopy-442vWG/JAFDTC (branch 442-vWG-1.5.0), downloads from that repository's
  releases (tag must match the version, file `JAFDTC Installer.msi`) and `VERSION.txt` is `v1.5.1`.

### Interface
- DSMS munition page: scroll bars so the Z-sled picture and lower fields are never cut off; the munition list now fits the window so
  BDU-50LGB is always reachable.
- New icons from DCS screenshots for BDU-33, BDU-50LD, BDU-50PO, BDU-50LGB and the APKWS rockets (M151 HE, M282 MPP), centered nose-first.

## Fixed
- **Mode 3/A** code entry: valid 4-digit codes (0000-7777, including leading zeros) are accepted and uploaded correctly.

## Known limits
- Flight plan: existing entries from 02 on are overwritten, not cleared. Only flight plan 1 is built. Page-down presses use extra waits so the CDU keeps up; very long waypoint lists take a while to upload.
- TAD range: assumes the jet starts at 0.06 nm and has the TAD realistic zoom levels mod installed (the 442 vWG mod).
- Battle Book values are copied as printed, including odd entries (B2 20LD1 qty 2, B2 20LD2 DTOF 5, 84 30LD1 MALT left blank).
- HA profiles assume CCIP delivery, the default.
- Profile creation and sorting on the jet assume the DSMS pages are in the default layout and that new profiles are added at the end of
  the profile list. Re-uploading after a default profile has been deleted on the jet cannot create new profiles for that munition.

## Files changed
Models/A10C: A10CMunition.cs, A10CDeviceManager.cs, DSMS/BattleBookProfiles.cs (new), DSMS/DSMSSystem.cs, DSMS/MunitionSettings.cs,
Misc/MiscSystem.cs, TAD/TADSystem.cs, Upload/DSMSBuilder.cs, Upload/MiscBuilder.cs, Upload/TADBuilder.cs, Upload/WYPTBuilder.cs;
Data/db-a10c-munitions.json; UI/Base: EditNavpointListPage.xaml(.cs), EditWaypointListHelperBase.cs, IEditNavpointListPageHelper.cs;
UI/A10C: A10CEditWaypointListHelper.cs, A10CEditDSMSMunitionSettingsPage.xaml(.cs), A10CEditDSMSPage.xaml, A10CEditMiscPage.xaml,
A10CEditTADPage.xaml; UI/App: ConfigurationListPage.xaml.cs, MainWindow.xaml(.cs), SettingsDialog.xaml(.cs); Utilities/FileManager.cs;
AppTypes.cs; JAFDTC.csproj; VERSION.txt; Images (icons, Images/zsled/*.png).
