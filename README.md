<h1> Aspiring Grand Strategy Game in Unity3d </h1>
Author: Justin D'Errico  <br>
Created: September, 2021 - Present <br>

<h2> Goal </h2>
I have always found the systems in grand strategy / 4X games interesting and wanted to replicate such systems with other areas of interest that I have included. I am putting an emphasis on learning new C# skills as well as efficient algorithms for the various parts of this project.

<h2> Interesting Aspects </h2>

<h3> Procedural Planet Generation </h3>
<h4> Inspiration </h4>
Games from Paradox Interactive, namely Europa Universalis IV inspired me to start this project as there have been talks about the next iteration in the franchise being set on a sphere rather than a 2D projection and that concept is incredibly inspiring to me, hence where I am at right now.
<h4> First Step, A Whole Lotta Reading </h4>
Before I started working on this project, I decided to sit down and look at some videos, blogs and papers about the project, learning from my past mistakes in getting sidetracked in projects inevitably leading to bogged down messes... <br>
Where I mainly draw much of my information on the topic is from this blog post: <br>
https://www.redblobgames.com/x/1843-planet-generation/ <br>
<br>
And videos by Sebastian Lague: <br>
https://www.youtube.com/watch?v=QN39W020LqU&ab_channel=SebastianLague <br>
<br>
As well as posts that Red Blob Games in their blog post draw from, currently: <br>
http://eveliosdev.blogspot.com/2016/06/plate-tectonics.html <br>
http://experilous.com/1/blog/post/procedural-planet-generation <br>
<br>
With all of this new information buzzing around in my head, I decided to tackle the first step in planet generation, creating a sphere.
<h4> Spheres Spheres Spheres </h4>
There are many approaches to procedural sphere mesh generation, most notably the UV Sphere and Icosphere. Both of these sphere types have issues with point distribution though, as they are other shapes expanded into the shape of a sphere. <br>
<br>
<p align="center"> 
  <img src= "Readme Additions/Icosphere UV Sphere Example.PNG"> <br>
  Top down example: UV Sphere on left, Ico Sphere on right
</p> <br>
The above example shows how the point distribution on each sphere differs. In the UV sphere, there is a higher level of detail (LOD) towards the poles, while the icosphere has a much more even distribution. <br>
An alternative approach to sphere generation is to expand a cube (a cube sphere as it were), which leaves an uneven distribution of points on the seams, rather than the poles as in the UV sphere. <br>
<br>
<p align="center">
  <img src= "Readme Additions/Cube Sphere.png"> <br>
  Expansion of Cube to Cube Sphere Example
</p> <br>
Definitely an improvement, but still worse when compared to the distribution on the icosphere. Now while the icosphere might seem as the best decision for point distribution, it definitely takes its toll (on the mind) in code complexity. To avoid this, I took a tip from the ever inspiring Sebastian Lague in one of his newer videos of planet generation (https://www.youtube.com/watch?v=lctXaT9pxA0&ab_channel=SebastianLague) to work with an octohedron rather than an icosahedron as a compromise between point distribution and code complexity, the results of which are...
<h4> The Octohedron Sphere </h4>
<br>
<p align="center">
  <img src= "Readme Additions/Octohedron.PNG"> <br>
  <img src= "Readme Additions/Octosphere.PNG"> <br>
  My personal implementation of the Octosphere
</p> <br>
Probably one of my favorite to personally implement as I took a different approach than in Sebastian's aforementioned video! By structuring the vertices array as a jagged array, I was able to simplify the code and improve readability (see OctahedronSphere class in MeshGenerator.cs). <br>
<p align="center">
  <img src= "Readme Additions/Octosphere Demonstration.PNG"> <br>
  Extremely Professional Visual Aide
</p> <br>
<br>
A short summary of how the structuring helped me generate the mesh, by having the jagged array mimic the positions of vertices in the octohedron, I am able to flatten the array int 1D as is required by Unity. (https://docs.unity3d.com/ScriptReference/Mesh.SetVertices.html) Setting triangles for the mesh also benefits from this structure as determining vertices for triangles is easy using the number of rows. <br>
<br>
<p align="center">
  <img src= "Readme Additions/Octosphere Triangulation.PNG" width=50%> <br>
  Coding Snippet of Triangulation <br>
  The conditional statement of (localUp.y > 0f) was necessary as when negative, the triangles were being rendered inverted. (Love triangulation...)
</p> <br>
As I've touched on some ideas that I have not explained but had to learn seperate from the materials I have already linked, here are some links that I hope help some people like they did for me. :grin: <br>
<br>
Unity meshes: https://docs.unity3d.com/Manual/AnatomyofaMesh.html <br>
Brief explanation of vertices, triangles and their orientations: https://youtu.be/ucuOVL7c5Hw?t=182 <br>
Winding order, determining front face of triangle: https://cmichel.io/understanding-front-faces-winding-order-and-normals <br>
<br>
Now while I am proud of my Octosphere implementation, I felt that the seams were very clearly noticable and would not do for what I planned for the project in the future and so I decided to implement a different kind of sphere generation that would give me even better distribution. While the icosphere would definitely be an improvement, I had in my mind the sphere type which Red Blob Games used in their blog post, one that had a very different implementation...
<h4> The Fibonacci Sphere (Of Nightmares) </h4> 
COMING SOON (There is a lot to write about this one and I need to focus on other things for a bit 🙂)
