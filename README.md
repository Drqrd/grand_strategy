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
<h4> The Spheres </h4>
There are many approaches to procedural sphere mesh generation, most notably the UV Sphere and Icosphere. Both of these sphere types have issues with point distribution though, as they are other shapes expanded into the shape of a sphere. <br>
