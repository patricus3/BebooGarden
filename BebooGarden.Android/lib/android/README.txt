FMOD's Android native libraries go here.

Without them Beboo Garden for Android will start, talk, unpack its sounds and then fail
the moment it asks FMOD for an audio system. Everything else works.

They are not in this repository because FMOD's licence does not allow redistributing them.

  1. Make a free account at https://www.fmod.com and go to Download.
  2. Take "FMOD Engine", platform Android. (The Core API is what this game uses.)
  3. From the archive, copy each api/core/lib/<abi>/libfmod.so into the matching
     folder beside this file:

       lib/android/arm64-v8a/libfmod.so      <- nearly every phone since about 2019
       lib/android/armeabi-v7a/libfmod.so    <- older 32-bit devices
       lib/android/x86_64/libfmod.so         <- emulators

  4. Rebuild. build_all.bat picks them up with no edit needed.

Use libfmod.so, not libfmodL.so - the L build is the logging one, and it is much larger
and slower. Match the FMOD version to lib/FmodAudio.dll in the Windows project, which is
binding version 2.02.

Only arm64-v8a is genuinely needed if you are just installing on your own phone.
