# Voxel Terrain

![Screenshot 1](https://raw.githubusercontent.com/barograf/VoxelTerrain/master/Screenshots/1.jpg)
![Screenshot 2](https://raw.githubusercontent.com/barograf/VoxelTerrain/master/Screenshots/2.jpg)
![Screenshot 3](https://raw.githubusercontent.com/barograf/VoxelTerrain/master/Screenshots/3.jpg)

## Introduction

This project's main goal is to generate and visualize terrain built using voxels. It was achieved using different approaches and computing technologies just for the sake of performance and implementation comparison.

## Used Technologies

There is a list of technologies used in this project:

* DirectCompute and CUDA for parallel terrain generation
* All sorts of DirectX 11 shaders (Vertex Shader, Pixel Shader, Geometry Shader, Hull Shader, Domain Shader, Compute Shader) used in visualization process
* HLSL for shaders implementation
* C# language for program logic and sequence algorithms implementation
* C language for CUDA kernels implementation

## Terrain Generation

The whole terrain generation process was implemented using sequence algorithms for CPU and then using parallel equivalents for Microsoft DirectCompute and Nvidia CUDA. Performance boost for parallel versions is significant and can be measured from tens to even hundreds.

Generation process utilizes the following algorithms and features:

* XORWOW algorithm implementation for random numbers generation
* Voxel weights generation using fractional Brownian motion (noise)
* Voxel weights generation using math equations
* Marching cubes for geometry extraction
* Prefix scan used for optimization of parallel generation

## Terrain Visualization

Visualization is made through DirectX 11. In order to obtain some eye candy effects the following algorithms were implemented:

* ambient occlusion
* tri-planar mapping
* displacement mapping
* bump mapping
* adaptive tessellation
* sky box
* linear fog
* post-processed bloom

Some screenshots presenting the final effect can be [found here](https://www.dropbox.com/sh/g1ybzkb11eumcy2/AAAbYdSYeN36YWkoxQe4qt2Qa?dl=0).

## Installation

There are some prerequisites:

* Graphics card with DirectX 11 and CUDA 2.0 or higher
* Microsoft.NET framework with version 4.0 or higher
* Microsoft DirectX SDK
* [CUDA Toolkit 8.0](https://developer.nvidia.com/cuda-downloads)
* Visual Studio 2015
* 64-bit platform due to cuRAND requirement

The solution uses some wrappers around native CUDA and DirectX libraries:

* [SlimDX](http://slimdx.org/) for DirectX - included in solution because there is no x64 equivalent in NuGet
* [ManagedCuda and CudaRand](http://managedcuda.codeplex.com/) for CUDA and cuRAND - shipped as NuGet packages

There are some sample textures in repository. Each one contains color, displacement and bump map.

## License

MIT
