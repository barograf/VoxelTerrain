# VoxelTerrain

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

There are some prerequisites before the installation process. This project requires a graphics card with DirectX 11 and CUDA 2.0 support. Moreover, there should be Microsoft .NET package installed in the system with version 4.0 or higher. It is also recommended to install Microsoft DirectX SDK package.

The solution uses some wrappers around native CUDA and DirectX libraries:

* [SlimDX](http://slimdx.org/) for DirectX
* [ManagedCuda and CudaRand](http://managedcuda.codeplex.com/) for CUDA and cuRAND

A pack of fancy textures needed in visualization process can be [found here](https://www.dropbox.com/s/zn8yypfrkocuyh0/VoxelTerrain%20Textures.7z?dl=0). Just download, unpack, and put them in project's folder before build. You can also place them in directory with binaries.

Compiled version of the project (without textures) can be [found here](https://www.dropbox.com/s/ja698cwdnjcz0ol/VoxelTerrain%20Binaries.7z?dl=0).

## License

MIT
