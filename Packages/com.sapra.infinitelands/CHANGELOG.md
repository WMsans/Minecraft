# [0.8.9] - 2025-07-29
## Added
- Graph: Drag and dropping an Edge will open the Create Node menu to add a node on the end
- Graph: New visualization mode based on Node Output
- Graph: Copy Node Guid pressing F2
- Backend: Improved node backend allowing a node to create more nodes
- Graph: Enabled Previewing of Height Map live on the scene
- Graph: Enabled Previewing of Point Instances live on the scene
- Graph: New event for before changes are done on a graph
- Graph: Lock Seed Node incorporates a Variable Seed mode
- Backend: GridData has been moved from custom class into its own nodes thanks to amplify

## Changed
- Core: Unity Terrains processors can be properly customized
- Graph: New edge colors for Height Data
- Graph: Simplified internal node methods
- Graph: Simplified biome node code
- Backend: GridData has been moved from custom class into its own nodes thanks to amplify

## Fixed
- Rendering: Shadows are no longer lost in older Unity versions
- Graph: Nodes that make use of textures, result in better sampling of data
- Graph: Changing a biome property that is inside a world graph will now properly update the parent graph
- Graph: Clear Node and other points nodes no longer break when opening the preview window
- Graph: Local Output properly works with lists
- Graph: Connecting a node to a previously invalid graph now properly triggers a regeneration of the graph
- Graph: Drag and Dropping a Terrain will properly create the required components
- Backend: Generation Distance no longer affects the Final Mesh Size
- Backend: Noise jobs don't break after a certain amount of octaves
- Backend: Properties shouldn't get lost after binding

## Removed
- Backend: Removed processors like Landmark Data, World Final Data, and Coordinate Data Holder

# [0.8.8] - 2025-06-21
## Added
- NEW NODE! Scale Node: Scale the input noise proportionally
- NEW NODE! Spawn Around Node: Create points around the origin of some previous points
- Graph: New shortcut, pressing Q will focus on the selected items
- Backend: Mesh Maker tests have been recovered and reimplemented
- Backend: More debugging options
- Documentation: New docs for the nodes created
- Documentation: Docs for Show in LOD and Auto Organize Terrains have been created

## Changed
- Graph: Nodes inside a graph will be in the center of the screen the first time the graph is created
- Samples: Snowy Mountains sample also includes a bottom grass part
- Documentation: Updated Roadmap

## Fixed
- Core: Landmark Placer will no longer create extra objects after domain reload
- Graph: Prefabs placed with Place Prefab node will have the prefab rotation taken into account
- Graph: Opening the "Create Node" window will be way faster than before
- Rendering: Unity 6.0 or higher will not throw errors from the vegetation shaders
- Backend: Opening a sample in unity 6.0 wil no longer lose references to textures and vegetation
- Backend: Undo after a graph is created will no longer delete the Height Output node
- Backend: Vegetation and Textures won't be lost after a domain reload

## Removed
- Samples: Cameras will no longer include missing scripts
  
# [0.8.7] - 2025-06-17
## Added
- Core: New debugging options for Vegetation Asset, including labels with better information
	- DrawLODBoundaries: Draws a circle around the maximum distance where an LOD is active
	- DrawTransitions: Draws circles where CrossFade transitions happen for LODs, Shadows and Maximum render Distance
	- DrawShadowBoundaries: Draws a circle where the shadows end
	- DrawSpawnedColliders:  Draws the place where a collider is spawned, their instanceID and the chunk where it is located
	- DrawItAsSpheres: Draws all the previous circles as spheres for better 3D 
- Core: Infinite Visualizer Draw Chunks will also draw their labels
- Core: Vegetation renderer allows to toggle the option to spawn colliders around the camera
- Core: Vegetation Asset allows customization of Collider Distance again
	- Minimal: Only 9 colliders will be generated around the player
	- By Distance: It will vary the amount according to **Collision Distance**
	- AllObjects: It will spawn all the colliders that it can for all the objects drawn
- Graph: Extra synonims into nodes

## Changed
- Core: Mesh Type configuration moved into the Mesh Maker 
- Core: More settings into View Settings configuration
- Documentation: Improved references to the vegetation renderer requirements to work properly

## Fixed
- Backend: Rotating the terrain will properly handle vegetation rendering
- Backend: Multiple terrain generators will no longer throw warnings
- Backend: Having Infinite Lands Terrain in a child will no longer be unresponsive
- Backend: Not having textures in the biome will no longer throw errors for vegetation
- Backend: Opening in big mode any window in the editor will no longer throw errors from the cameras

# [0.8.6] - 2025-06-09
## Added
- NEW NODE! Layers Node, allows organizing height maps with a priority ordering
- NEW NODE! Lock Seed, allows locking nodes to use the same seed
- Core: Terrain Painter includes an option to use a custom texture resolution. This can improve runtime performance
- Core: Fast Initialization variable in Infinite Visualizer. Allows travelling through all the nodes without any limit during initalization
- Core: Max Steps in Tree variable in Infinite Visualizer. How many steps can we do inside a tree before waiting a frame.
- Graph: Curve Nodes include a Global curve mode
- Docs: More references to the Vegetation Shader requirement

## Changed
- Core: CustomTextureResolution from Mesh Settings renamed to CustomSplatMapResolution
- Graph: Find by Tag node no longer requires a GridSize variable
- Graph: Improved code for Reroute Node
- Graph: Improved code for Missing Node
- Graph: Apply Mask will now create an internal branch, avoiding the processing of the Input if necessary.
- Backend: Mesh Maker doesn't produce Garbage at runtime
- Backend: Terrain Visualizer doesn't produce Garbage at runtime
- Backend: Huge update on how the node system functions, allowing for nodes to take multiple steps.
	_This was a necessary step to start the process of optimization. Allowing multiple steps in a node ensures thatwe can wait to get info before continuing. This makes sure that, for cases with an ApplyMask, we can avoid a whole chain of nodes by checking if there's even any value on the Mask._
- Docs: Improved general documentation, reflecting the new changes


## Fixed
- Core: Texture Assets now update the rendering when changing their settings.
- Graph: Creating a Reroute Node will make it end closer to the mouse
- Graph: Edges now properly appear when pasting nodes
- Graph: Copy and Pasting nodes from a Combine Node no longer produce duplicates or missing edges.
- Graph: AppplyMask renamed to ApplyMask
	_This means that the nodes will appear as missing. Please recreate those parts with the proper ApplyMask node_
- Graph: Nodes that have ports that can be hidden will properly be updated automatically
- Graph: List ports are properly handled, being updated when they are needed.
- Backend: LODGroups will make use of Shared Materials, instead of directly using the Materials Array

# [0.8.5] - 2025-05-09
## Changed
- Core: Floating Origin incldues global locks

# [0.8.4] - 2025-05-04
## Added
- Documentation: New Tutorial on how to apply grass displacement

## Changed
- Core: Floating Point will be automatically added to chunks if Floating Origin is detected

## Fixed
- Core: Grass Interactions is now properly created
- Core: Floating Origin and Floating Point are properly detected by Vegetation System
- Core: Infinite Generation will no longer break when modifying the graph
- Core: Changing an asset at runtime will not break the infinite generation
- Documentation: Align with Terrain and Stay in Ground images have been updated to reflect it's current state

# [0.8.3] - 2025-04-12
## Added
- Rendering: Added dither when close to the camera to the shaders

## Changed
- Core: Samples tree colliders are now properly created

## Fixed
- Graph: Sticky Notes now also store size, theme and text size
- Backend: Biome nodes are now initialized and have proper creation on the first time

## Removed
- Backend: Naughty attributes fully removed from website and Thrid Party Notice
  
# [0.8.2] - 2025-04-09
## Added
- Components: Multi-point generation!
- Backend: New attributes replacing the used ones from Naughty Attributes

## Changed
- Graph: Jitter node now adds the modifications instead of overriding
- Core: Landmark placer will now schedule spawns of objects in multiple frames instead of all at once

## Fixed
- Core: Vegetation updates properly an on time, removing the chance of vegetation out of place or wrong color.
- Core: Vegetation splat maps are properly created, so that the color space is linear, and thereofre it closely matches with the generated maps.

## Removed
- Backend: Naughty Attributes has been completely removed
*The main reason being that I was no longer using the whole potential of Naughty Attributes. After the big graph changes it was prefereable to have a custom implementation of the attributes I was using to simplify the whole node system*

# [0.8.1] - 2025-04-03
## Fixed
- Graph: Sticky notes are now properly saved with the graph

# [0.8.0] - 2025-04-02
## Added
- NEW NODE! Asset Output, replaces Vegetation Output and Texture Output
	*They were already doing the same, having them together makes bug hunting overall simpler.*
- NEW NODE! Reroute Node, allows redirecting connections within the graph
- NEW NODE! Repeated Mask Node, allows the creation of pattern masks
- NEW NODE! Position Node, allows getting positional data of each vertex in world units
- NEW NODE! Color Output, allows sampling colors directly for quick prototyping
- NEW NODE! Random Noise, allows random values to be generated
- NEW NODE! Grid Node, allows creation of points inside a grid
- NEW NODE! Find By Tag Node, allows importing points from the scene to the graph
- NEW NODE! Clear Points Node, allows the deletion of points inside a mask
- NEW NODE! Flatten Around Points Node, allows the flattening of terrain around a point
- NEW NODE! Jitter Points Node, allows the randomization of points
- NEW NODE! Place Prefab Node, allows the placement of prefabs in specific points
- NEW NODE! Points To Density Node, allows the creation of density masks from points
- NEW NODE! Shapes Node, allows the creation of simple shapes
- NEW NODE! Texture Node, allows importing textures to be sampled and used within the graph
- NEW NODE! Move Origin Node, allows the modification of the center of any inputted data
- NEW NODE! Global Output, allows biomes to export variables
- NEW NODE! Global Input, allows biomes to use external variables
- NEW NODE! Assets Mask Node, allows the automatic masking of all asset outputs
- Components: Infinite Lands Terrain, it now handles the terrain generation
- Components: New component ApplyInstanceTexturesToParticle that provides texture data to particle systems
- Components: New Component Show In LOD. Ensures that only certain objects at the lod or lower are enabled
- Components: Support for drag-and-dropping assets into the scene hierarchy
- Graph: New options of height map visualization, now supporting global edges or local edges (giving more clear view for low frequency noises)
- Graph: New Synonims. The add node searching window is now easier to navigate
- Graph: New Shortcut, press F1 to open documentation
- Graph: New shortcut, press N, S or G, to create a new node, a sticky note or a group
- Graph: The graph inspector includes an array of the textures being used inside that graph.
- Graph: Proper support for node validation to give better insights on what's wrong
- Core: Vegetation Asset, replaces GPU Instancing and CPU Instancing
- Core: Asset Pack, allows configuration and placement of multiple assets in one go
	*Settings between the two were a bit complicated. Now that the backend has been unified, the type of assets can be handled directly from the same Scriptable Object*
- Core: Vegetation has now methods available to modify its settings.
- Core: Vegetation has now methods available to modify its cameras.
	*Although there is multi camera setup available, newly generated cameras at runtime need to make use of those methods to get vegetation rendering working on them.*
- Core: Vegetation Asset now have settings for how to sample terrain color.
- Core: Vegetation Asset has a new setting to modify its height via a Simplex Noise pattern.
- Core: Vegetation Asset can now be removed when a specific texture is sampled
- Rendering: Support for hand-written shaders
- Rendering: Support for Quad-Tree generation of Unity Terrains
- Backend: New interfaces to customize the different ports of a node
- Backend: Proper customization for previews
- Backend: Proper system for the resulting generated data
- Backend: New buttons to export a Unity Terrain into the project Assets folder
- Documentation: TVE (The Visual Engine) now works with Infinite Lands
- Documentation: New page for compatibility
- Documentation: New nodes documentation
- Documentation: New tutorials
- Documentation: [Tip Jar](https://ko-fi.com/sapra)
	*I've opened a little support page for those who want to help out even more. Honestly, just buying Infinite Lands already means a lot. You’re a big part of what keeps me going. But keeping things running, pushing out updates, and working on this every day gets tough when sales slow down and bills keep piling up. So if you ever feel like tossing in a little extra support, it’d mean the world to me. Either way, I appreciate you being here, whether you donate or not.*

## Changed
- Graph: Vegetation Output and Texture Output have been merged into a single Asset Output node
- Graph: Improved copy pasting of nodes, edges, groups and sticky notes
- Core: Simplified process to add new, out of scope textures
- Core: Improved settings menu of Vegetation Asset
- Core: Modified samples to reflect changes
- Rendering: Simplified process to create custom shaders
- Backend: Improved speeds of real-time editor.
- Backend: Many improvements into memory management to be more lightweight and dynamic.
- Backend: Full graph editor rework
	*One of the most necessary changes. With the addition of point generation it was necessary to rework the graph system to allow that kind of modularity.
	With these changes, creating new nodes unrelated to the current workflow should be simpler to do.*
- Backend: Improved the workflow to create new nodes
- Backend: Updated About Window to follow better styling
- Documentation: Tweaked documentation to reflect changes

## Fixed
- Graph: Improved performance of previews
- Graph: Two warp nodes together no longer crash the graph
- Core: No longer gets stuck when creating/moving/renaming a texture 
- Core: Certain format of textures no longer break the texturing of the terrain
- Core: Proper support for multiple cameras in Vegetation. No longer needed to setup since vegetation will be rendered in all necessary cameras.
- Core: CPU Instancing is now working on the Editor
- Core: Vegetation texture sampling matches the terrain.
- Rendering: Normal maps in the terrain material are now properly handled.
- Backend: Default chunk at (0,0,0) will no longer receive data when in Infinite Mode
- Backend: Runtime garbage collection reduced to 0

## Removed 
- Components: Infinite Lands Visualizer and Single Chunk Visualizer
	*A new proper way of handling the generation was required, so these two components have been removed to give space for Infinite Lands Terrain, that should be easier to bug fix and maintain*
- Components: Quad Chunk and Single Chunk
	*With the new Infinite Lands Terrain, it was no longer to differentiate these two cases as a specific object, now it's all handled via the main component*
- Graph: Vegetation Output and Texture Output have been removed, replaced by Asset Output
- Core: GPU Instancing and CPU Instancing have been removed, replaced by a normal Vegetation Asset

# [0.7.5] - 2024-12-19
## Added
- Backend: Unity Terrain experimental support
	*Another feature that it's been a while that I wanted to add. This is still not feature complete since it's lacking Trees and Grass from Unity Terrain. Vegetation can be included by using the default system. We will see how this goes and what needs to be tweaked*
- Backend: Warning when pressing Regenerate on a graph without any chunk visualizer attached
- Backend: Warnings when launching a world without a terrain setup
- Backend: Warnings when launching a world that has floating origin but the chunk doesn't have floating point
- Documentation: Roadmap to the package.  https://ensapra.com/packages/infinite_lands/roadmap
	*It's been a while since I wanted to add a roadmap to the website. I just wasn't sure what was the best way to do it. This is an experimental roadmap. It contains ideas and tasks that I want to add at one point or another, but being there doesn't mean they will. It's just that they are on my radar*

## Changed
- Rendering: Vegetation max render distance can now be modified in runtime
- Backend: Removed deprecated methods
- Backend: Workflow of custom chunks has been modified to be more intuitive and simpler.
	*This means **redownloading samples**. Now the chunks will be a child of the terrain generator. Anything that goes with the chunk will be spawned next to it. You can also set a prefab if you prefer in the Infinite Chunk Visualizer. That should make it easier for custom systems*
- Documentation: NEW DOMAIN! Yay, no longer to ensapra.github.io but now we can go to **ensapra.com!**

## Fixed
- Core: Made Vegetation displacer be disabled by default in URP and HDRP
- Core: Disabling samples features that made it look wrong
- Rendering: Minimal shader looks properly
- Backend: Allows for any texture, any resolution and even missing data
- Backend: Drag and drop now works properly
- Backend: Colliders sometimes weren't properly generated because of nan values
- Backend: Scenes no longer save with meshes and materials, reducing considerably all sizes
- Backend: General fixes

## Removed
- Core: Removed settings from the samples

# [0.7.4] - 2024-12-10
## Added
- NEW FEATURE!: Floating Point. Allows for infinite movement while generating the terrain without losing precision. It includes three components:
	- Floating Origin: Should be added on the main generator
	- Floating Point: Should be added to objects that should stay in a specific position
	- Floating Particle: Should be added to objects containing a particle system
- NEW FEATURE!: Environmental Components. Allow for objects manually placed in the terrain to take into account the world generation
	- AlignWithGround: When a new mesh is generated in it's position, it will align with the groun
	- StayOnTerrain: Ensures that the object doesn't go underthe ground when a new mesh is generated. 
- Graph: New blending values for textures and vegetation
- Graph: Relativity on feature extraction. You can choose in relation to what should features exist. Relative to the terrain or to vector up. (Useful for cases where the generation has some rotation)
- Editor: Test scripts for various components

## Changed
- Core: Due to changes in the PointStore, some scripts in the samples required updating
- Core: Samples make use of floating point origin and the other new environmental components
- Core: Huge improvement on rendering of vegetation in small meshes. You can now use way smaller meshes without stutters
- Backend: Extra null checks on events
- Backend: Moved data into it's own interface

## Fixed
- Core: Objects no longer pop in and out of existence as frequently as before
- Rendering: Displacemnt shader now works properly when disabled
- Backend: Some chunks were being rendered when they shouldn't have

## Removed
- Backend: No more references to player in PointStore

# [0.7.3] - 2024-10-23
## Added
- NEW NODE: Directional Warp. Warp a node according to their normal map, creating smaller detail
- Components: Vegetation Renderer has a new parameter called Maximum Render Distance that allows to limit the rendering of all vegetation
- Rendering: New Minimal Shader. Contains the absolut basic configuration for any shader to work with the GPU System that Infinite Lands provides.
- Core: Assets now reload the vegetation for live editing

## Changes
- Backend: IHoldVegetation and Vegetation Asset slightly decoupled

## Fixed
- Core: No longer breaks without a mesh or without a material
- Rendering: Correct sampling of normals
- Backend: Nodes that make use of an Animation Curve now reload after changes have been done to it. 
- Backend: Normal maps in shaders now are correctly set
- Backend: All LODs now have the chance to generate colliders
- Backend: Now chunks shouldn't end up with a mesh collider enabled but without an actual mesh set for it

# [0.7.2] - 2024-10-7
## Added
- As per request of unit577 and stor314. Backend: Support for any rotated type of terrain
- Backend: Support for simple grid like generation
- Backend: Moved Mesh Creation into a separate interface and MonoBehaviour

## Fixed
- Backend: Fixed a problem in code where building the project would break
- Backend: Added missing events in Texture Painter for when textures are generated or removed
- Backend: Export popup not correctly having the generator and therefore not working
- Backend: Creating a simple world no longers throws an errors and creates two output height Generators
- Backend: Texturing doesn't break when there are no textures available
- Backend: Removed deprecated node from Simple World asset generator
- Backend: Resolved the issue of constantly checking for new data in vegetation system
- Backend: Resolved an issue where the vegetation would fall with a wrong offset
- Backend: Reduced vegetation complexity of the diferent scripts
- Backend: Biome generation now works
- And more bugs that I found along the way

## Changed
- Backend: Removed unnecessary fields in CPU Instancing
- Backend: Renamed Size to Minimum Scale and Maximum Scale in CPU Instancing and GPU Instancing assets. 
	*It wasn't super clear that Size was actually a range of values, so this new name should better reflect that*

# [0.7.1] - 2024-09-30
## Changed
- Rendering: Changed impostor ambient occlusion so light reacts accordingly

## Fixed
- Core: Created a Shared Content folder for all the assets of the samples
- Core: Samples have been fixed so that they correctly load up when importing them

# [0.7.0] - 2024-09-29
## Added
- Components: A new component called Texture Painter. This new component manages the texturing of the terrain and should be used when requiring color data of the generated chunk.
- Graph: Added Minimum, Maximum, To Zero to Multiply Node (now named Apply Mask).
		This should make it more clear to differentiate between the Normalized Multiply option from Combine and the functionality that this node does. 
- Graph: A fixed seed option was added for the Voronoi Generator.
		This option should allow users to use the variants of the Voronoi Generator with the correct context. 
- Core: New Vegetation Type, CPU Instancing.
		This new type of vegetation will use the native Unity Instantiate method to create new game objects and pool them accordingly. This method is slower than GPU Instancing but might be interesting for anyone looking at persistence.
- Core: Added new Samples
	-  World Generation (Built-in)
	-  World Generation (URP)
	-  World Generation (HDRP)
	- Grass Displacement (Built-in)
- Editor: Multi-window support
- Editor: Save and restoring of graph positions
- Editor: Copy pasting of group
- Editor: Creation of sticky nodes, with copy-pasting functionality
- Editor: Custom icon in the windows
- Editor: New options in the context menu
	- Copy Properties: Allows copying properties of a node to be pasted into another
	- View option to graph view so that it focuses on the right part
		- Expand/Collapse Nodes.
		- Expand/Collapse Preview.
		- Fit: Allows the graph to focus on all the nodes.
- Editor: New Export Window.
	- Export Mode: Allows selection of the exporting method. Check out the different exporters and the documentation to create your exporter. 
	- Quality: Allows generation exporting to a higher quality resolution than it was previously. 
- Editor: New Drag and Drop functionality for assets. You can drag an asset into the Editor Window to automatically create the required node. 
- Backend: A Mesh Collider will be automatically added when Entering Play Mode in Single Chunk Visualizer
- Backend: New Drag and Drop from Inspector View to Scene View to generate the necessary components
- Experimental: New option in Mesh Settings to export masks in a custom texture Resolution.
		This new feature allows you to set any resolution during the creation of Texture Masks and Vegetation Masks. However, when enabled, generation times could be doubled or more.
	
## Changed
- Editor: Resolution and mesh scale in the editor will now be clamped after imputing a value
- Editor: Fully reworked the node creation menu. Now it simulates the Shader Graph search menu. 
- Graph: Separated Noise Nodes into different nodes.
		The reason for this change is to accommodate for future nodes. I'm planning on adding more variations of noise and I would like a more robust and clear way to have each type defined. 
- Graph: Renamed Normal Mask to Select Mask
- Graph: Renamed Multiply Node to Apply Mask
- Graph: Variables in Nodes and Generator are now Read Only (can be disabled by going into Debug Mode of Unity)
- Graph: Added preview options to Texture Nodes and Vegetation Nodes
- Rendering: Improved and reduced memory consumption.
- Rendering: Moved all shaders into Shader Graph, making them compatible with URP and HDRP
- Rendering: Fully reworked the rendering system to be more performant and compatible with URP and HDRP
- Backend: Vegetation will now be rendered in all the cameras. 
- Backend: Instead of selecting the cameras via a List, now you can set a Culling Mask that will be used to select the cameras that render that channel.
- Backend: Caching normal map generation for faster generation times
- Backend: General renaming of classes, variables, and namespaces
- Backend: Added more interfaces to allow users for self-implementations of assets and nodes (Check Documentation)
- Backend: Reworked Point Store to be more efficient and consistent. 
- Backend: Deleting a generator will now also close the window where it was opened

## Fixes
- Editor: When deleting a group it no longer deletes all the nodes
- Editor: Groups will always stay grouped
- Editor: Height Output Node should always be generated now
- Rendering: Shadows of GPU Instanced objects should no longer disappear when the object is out of frame. 
- Rendering: LOD Transitions should always work
- Backend: General precision issues have been fixed. Now Generation between Burst compiled enabled or disabled shouldn't be too different.
- Backend: Undo/Redo no longer breaks the graph editor.
- Backend: Auto-Update no longer breaks other generations. 
- Backend: Copy-pasting is now more responsive, and faster, takes more objects into account, and can be done between graphs. 
- Backend: When saving an asset, the generation window will no longer be black and it will keep the data consistently
- Backend: Pressing play in a scene with an Infinite Generator and the Graph Editor open should no longer break the graph/scene. 
- Backend: Null Texture or Null Vegetation Assets will no longer crash the graph or Unity
- Backend: Improved validation of Nodes so that it is more robust.
- Backed: General fixes in the Decimated Mesh mode to always work.
- Backend: General bug fixes
- Backend: General stability fixes
- And many many more that I might not have been able to track...

## Deprecations
- Graph: Noise Generator node has been deprecated. You will see it in red. Please change it to the corresponding node before the next major release.
- Renderer: **All previous shaders have been removed!** Please make sure to select the new shaders accordingly.

# [0.6.3] - 2024-07-19
## Fix
- Graph: Texture and Vegetation don't crash unity anymore when there's no connection
- Backend: Reworked the validation of nodes to be more robust
- Backend: General bug fixes
- Backend: General stability fixes

# [0.6.2] - 2024-06-03
## Fix
- Backend: General bug fixing to allow non-breaking generation

# [0.6.1] - 2024-05-28
## Fixed
- Graph: Cavity Node properly generates the border
- Core: Corrected the sample to look slightly better and have textures in all the places
- Backend: Removed GetIndices method
- Backend: Finally fixed the generation of images

# [0.6.0] - 2024-05-26
## Added
- Added a Mask into the Warp Node
- Added Copy/Paste/Duplicate funcionality
- Added GetSlope node
- Added GetCavity node
- Added Blur Node
- Added the documentation button into each node
- Modifying a biome will also modify the terrain if both auto update are setup
- Support for Unity 2021.3
- Added export to image into the node editor
- Added About Window inside InfiniteLands/About

## Changed
- Renamed HeightMask into RangeMask
- Renamed CurveMask into Gate
- Renamed SlopeMask into NormalizedMask, no longer extracts the slope from the map
- Remap node separated into three nodes
    - Normalize 
    - New Range 
    - Curve 
- Warp node only uses one warp channel
- Added a range into lacunarity field in noise node
- Backend rework allowing for more modularity and many bug fixes
- Toggle the icon of visualize texture

## Fixed
- Step filter missing reference fix
- Fix octaves in noise to handle any amount of octaves without breaking
- Fixed Interpolate Node having the wrong minmax values (outputing wrong results) 

# [0.5.1] - 2024-05-21
- Fix on Terrain Chunk. Null reference exception removed
- Samples are now enabled by default

# [0.5.0] - 2024-05-15
- First upload!