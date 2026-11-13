# JAFDTC 1.5.0 (442 vWG fork by Snoopy) - Change Log since 1.4.0

Airframe: A-10C II. Build date 12-Nov-26.

## New
### DSMS - dumb bomb support
- **CBU-87** now has DES TOF (OSB 19) and MIN ALT (OSB 18) profile fields.
- New munitions in the DSMS munition list: **Mk-82, Mk-82 Air, Mk-84, BDU-33, BDU-50LD, BDU-50PO, BDU-50LGB**.
- New profile fields (where the munition has them): **DES TOF, MIN ALT, MIN TOF, HD TOF, LD TOF**.
  TOF values accept decimals from 0.00 to 99.9.
- Release mode can be changed on Mk-82 Air, BDU-33 and all three BDU-50 types.
- GBU-54: **Impact Azimuth (IMP AZ)** and **Impact Angle (IMP ANG)** can be set and are uploaded.
- Escape Maneuver option shown as **CLM** (was CLB) to match the jet.

### DSMS - multiple named profiles and the Battle Book
- A munition can now have **multiple profiles**: the jet's default profile plus any number of custom, named profiles.
- **Profile** drop-down on the munition page; **New...** creates a profile with a name you choose (1-8 characters: A-Z, 0-9, space, period); **Delete** removes it.
- Custom profiles are saved in the JAFDTC configuration and are **created on the jet at upload**
  (MK-xx > VIEW PRO > type name on the CDU > NEW > settings > SAVE).
- **"Add Battle Book profile..."** drop-down adds a profile from the v303 Battle Book (with a confirmation prompt) and fills in
  DTOF/HD TOF, MALT, ripple quantity and interval, escape maneuver, fuze and release mode.
  75 Battle Book profiles: Mk-82 (15), Mk-82 Air (9), Mk-84 (6), CBU-87 (6), BDU-33 (15), BDU-50LD (15, named "50 ..."),
  BDU-50PO (9, named "50A...").
- The **Z-sled picture** for the selected Battle Book profile is shown beside the settings.

- **Profile order:** the default profiles named MK-82, MK82APO, BDU-33, BDU-50PO, BDU-50LD and BDU-50LGB are moved to the
  **end** of the DSMS profile list on upload (not deleted). When a munition has custom profiles the whole list is also sorted:
  Precision (GBUs, CBU-103, CBU-105), General Purpose, Rockets, Maverick; within a munition TOSS, diving high-altitude (HA)
  then low (DB, LD), steep to shallow, release quantity high to low. A profile order set on the DSMS Profile Order tab is used
  for the munitions instead. Without custom profiles nothing else is reordered.
- **Data folder setting:** Settings now has a *Data folder* box with Browse... and Default buttons. It selects where JAFDTC
  keeps configurations, settings, databases and kneeboards (default Documents\JAFDTC) and can copy existing files to the new
  folder. JAFDTC closes and uses the new folder when started again. The choice is stored in %LOCALAPPDATA%\JAFDTC\datafolder.txt.
- Update check now reads `VERSION.txt` from github.com/Snoopy-442vWG/JAFDTC (branch 442-vWG-1.5.0) and downloads from that
  repository's releases (tag must match the version, file `JAFDTC Installer.msi`). `VERSION.txt` is v1.5.0.
- DSMS munition settings page scrolls (vertical and horizontal scroll bars) so the Z-sled picture and lower fields are never cut off.
- The title bar shows the version (1.5.0 (442 vWG)).

### Misc - MFCD
- Left and right MFCD **OSB 12-15 page assignments** (COMM, STAT, MAV, CDU, MSG, DSMS, TGP, TAD) can be set and are uploaded.
  After the layout is set, OSB12 is pressed on the left MFCD and OSB13 on the right MFCD to return to the pages.

### Interface
- New icons for BDU-33, BDU-50LD, BDU-50PO, BDU-50LGB and the two APKWS rockets (M151, M282).
- About box and version updated: 1.5.0, "442 vWG fork by Snoopy".

## Fixed
- **Mode 3/A** code entry: valid 4-digit codes (0000-7777, including leading zeros) are accepted and uploaded correctly.

## Known limits
- Battle Book values are copied as printed, including odd entries (B2 20LD1 qty 2, B2 20LD2 DTOF 5, 84 30LD1 MALT left blank).
- BDU-50 profiles use the Mk-82 / Mk-82 Air values.
- Profile creation on the jet assumes the DSMS pages are in the default layout.

## Files changed
Models/A10C: A10CMunition.cs, DSMS/BattleBookProfiles.cs (new), DSMS/DSMSSystem.cs, DSMS/MunitionSettings.cs,
Misc/MiscSystem.cs, Upload/DSMSBuilder.cs, Upload/MiscBuilder.cs; Data/db-a10c-munitions.json; UI/A10C: A10CEditDSMSMunitionSettingsPage.xaml(.cs),
A10CEditMiscPage.xaml; UI/App/ConfigurationListPage.xaml.cs; AppTypes.cs; JAFDTC.csproj; Images (icons, Images/zsled/*.png, 75 pictures).
