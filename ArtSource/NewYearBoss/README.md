# Sleigh Thief — Blender source

Source: SleighThief.blend (Blender 4.5.3). Rebuild script: tools/assets/build_sleigh_thief167.py, run from project root. This is a static modeling blockout, no armature or animation yet.

Blender source uses Z up, nose -Y, meters. The script saves this editable source before exporting. For FBX only, geometry and object positions are rotated 180 degrees around Blender Z; the orientation is baked without putting a corrective rotation on the Unity visual root. Export uses Z Forward / Y Up, FBX Units Scale; Unity import uses Convert Units and Bake Axis Conversion. Verified Hull_Front minus Hull_Rear points +Z in Unity and visual root scale is 1. Do not remove this tested conversion solely to match a generic export preset.

FBX and separate Unity materials: Assets/Events/NewYear/Models/. Unity visualization prefab: SleighThief_Visual.prefab. Static preview is on LevelEvent at (9,4,10), rotation Y=210. Existing BossSpawner remains unchanged. Geometry: 1896 triangles, 78 separate renderers, 8 color materials. Renderer count needs optimization after shape review; current separation preserves editable parts for rigging.

A future gameplay prefab must be separate from this static preview and include validated damage, attacks, telegraphs, persistence and network behavior. Visual gifts are not gameplay projectiles.

DB-ON-245 expression: angry eyebrows are saved as Unity overrides in the shared SleighThief_Visual.prefab, then copied into the combat prefab. Imported parent axes invert the apparent slope; verified local Y is +22 degrees for Brow_-1 and -22 for Brow_1 (inner tips 0.135 m below outer tips). Do not replace the shared visual with a fresh raw FBX instance when rebuilding gameplay assets. The Blender source and FBX were inspected but not overwritten for this correction. Original procedural chuckles and throw sound: tools/assets/build_sleigh_audio245.py; audio245.json records provenance and WAV format.

DB-ON-246 used a generic Windows SAPI Russian 'хи' syllable assembled into hi-hi. This was rejected as spoken syllables; its files and generator are historical.

Current DB-ON-247 voice uses complete performed chuckles by remarknl, Freesound 197411, CC0. Audio247 contains the public HQ preview, license/source evidence and decoded audio. Run tools/assets/build_sleigh_laugh247.py with Blender Python to rebuild three whole excerpts into the existing Unity WAV paths; audio247.json records processing, exact times and hashes. No text-to-speech, syllable repetition or pitch shifting. The throw sound and Unity GUIDs are unchanged. The 245/246 generators would restore rejected earlier sounds if run by themselves.

Normals correction DB-ON-168: both runners, the right ear and nose had inward normals. All solids now have consistent outward winding; hat base is capped and inner-ear inlays have 0.012 m thickness. All 78 meshes pass closed-manifold, positive signed volume, non-degenerate faces and zero further normal-change checks. Reports: normals168-before.json / normals168-after.json. Generator includes the same repair stage before saving/exporting. Updated Unity evidence: Docs/locations/previews/sleigh-thief-normals168.png and sleigh-thief-underside168.png. Earlier *167.png images are pre-fix history.
