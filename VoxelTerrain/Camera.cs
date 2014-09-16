using System;
using SlimDX.Direct3D11;
using SlimDX;

namespace VoxelTerrain
{
    /// <summary>
    /// Allows observing the scene with the mouse and keyboard.
    /// </summary>
    public class Camera
    {
        /// <summary>
        /// Virtual adapter used to perform rendering.
        /// </summary>
        private Device graphicsDevice;

        /// <summary>
        /// Gets or sets position of a camera.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }

            set
            {
                position = value;
                UpdateView();
            }
        }
        private Vector3 position;

        /// <summary>
        /// Gets or sets a point the camera look at.
        /// </summary>
        public Vector3 Look
        {
            get { return look; }

            set
            {
                look = value;
                UpdateView();
            }
        }
        private Vector3 look;

        /// <summary>
        /// Gets a vector indicating up direction.
        /// </summary>
        private static readonly Vector3 up = new Vector3(0, 1, 0);

        /// <summary>
        /// Gets or sets a vector which is used in a look vector spherical mapping.
        /// </summary>
        private Vector2 spherePoint;

        /// <summary>
        /// Gets or sets a near plane of a camera frustum.
        /// </summary>
        public float NearPlane
        {
            get { return nearPlane; }

            set
            {
                nearPlane = value;
                UpdateProjection();
            }
        }
        private float nearPlane;

        /// <summary>
        /// Gets or sets a far plane of a camera frustum.
        /// </summary>
        public float FarPlane
        {
            get { return farPlane; }

            set
            {
                farPlane = value;
                UpdateProjection();
            }
        }
        private float farPlane;

        /// <summary>
        /// Gets or sets a field of view of a camera.
        /// </summary>
        public float FieldOfView
        {
            get { return fieldOfView; }

            set
            {
                fieldOfView = value;
                UpdateProjection();
            }
        }
        private float fieldOfView;

        /// <summary>
        /// Gets a view matrix of a camera.
        /// </summary>
        public Matrix View
        {
            get { return view; }
        }
        private Matrix view;

        /// <summary>
        /// Gets a projection matrix of a camera.
        /// </summary>
        public Matrix Projection
        {
            get { return projection; }
        }
        private Matrix projection;

        /// <summary>
        /// Gets a view matrix multiplied by a projection matrix.
        /// </summary>
        public Matrix ViewProjection
        {
            get { return viewProjection; }
        }
        private Matrix viewProjection;

        /// <summary>
        /// Gets plane equations used in frustum culling.
        /// </summary>
        public Vector4[] FrustumPlanes
        {
            get { return frustumPlanes; }
        }
        private Vector4[] frustumPlanes;

        /// <summary>
        /// Determines camera movement speed.
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// Creates a camera with specified values.
        /// </summary>
        /// <param name="graphicsDevice">Virtual adapter used to perform rendering.</param>
        /// <param name="position">Position of a camera.</param>
        /// <param name="look">A point the camera look at.</param>
        /// <param name="nearPlane">A near plane of a camera frustum.</param>
        /// <param name="farPlane">A far plane of a camera frustum.</param>
        public Camera(Device graphicsDevice, Vector3 position, Vector3 look, float nearPlane, float farPlane)
        {
            this.graphicsDevice = graphicsDevice;
            this.position = position;
            this.look = look;
            this.nearPlane = nearPlane;
            this.farPlane = farPlane;
            this.fieldOfView = (float)Math.PI / 3;
            this.frustumPlanes = new Vector4[4];
            this.Speed = 20.0f;

            UpdateView();
            UpdateProjection();
            FixVectors();
        }

        /// <summary>
        /// Updates camera view matrix.
        /// </summary>
        private void UpdateView()
        {
            view = Matrix.LookAtLH(position, look, up);
            viewProjection = view * projection;
            ExtractPlanesFromFrustum();
        }

        /// <summary>
        /// Computes plane equations used in frustum culling.
        /// </summary>
        private void ExtractPlanesFromFrustum()
        {
            // Left clipping plane.
            frustumPlanes[0].X = viewProjection.M14 + viewProjection.M11;
            frustumPlanes[0].Y = viewProjection.M24 + viewProjection.M21;
            frustumPlanes[0].Z = viewProjection.M34 + viewProjection.M31;
            frustumPlanes[0].W = viewProjection.M44 + viewProjection.M41;

            // Right clipping plane.
            frustumPlanes[1].X = viewProjection.M14 - viewProjection.M11;
            frustumPlanes[1].Y = viewProjection.M24 - viewProjection.M21;
            frustumPlanes[1].Z = viewProjection.M34 - viewProjection.M31;
            frustumPlanes[1].W = viewProjection.M44 - viewProjection.M41;

            // Top clipping plane.
            frustumPlanes[2].X = viewProjection.M14 - viewProjection.M12;
            frustumPlanes[2].Y = viewProjection.M24 - viewProjection.M22;
            frustumPlanes[2].Z = viewProjection.M34 - viewProjection.M32;
            frustumPlanes[2].W = viewProjection.M44 - viewProjection.M42;

            // Bottom clipping plane.
            frustumPlanes[3].X = viewProjection.M14 + viewProjection.M12;
            frustumPlanes[3].Y = viewProjection.M24 + viewProjection.M22;
            frustumPlanes[3].Z = viewProjection.M34 + viewProjection.M32;
            frustumPlanes[3].W = viewProjection.M44 + viewProjection.M42;

            // Normalize the plane equations.
            frustumPlanes[0].Normalize();
            frustumPlanes[1].Normalize();
            frustumPlanes[2].Normalize();
            frustumPlanes[3].Normalize();
        }

        /// <summary>
        /// Fixes camera sphere point and look vector. Sphere point is fixed by camera look vector spherical unmapping.
        /// Look vector is fixed by putting it at a distance of one unit away from position vector.
        /// </summary>
        public void FixVectors()
        {
            Vector3 direction = look - position;
            direction.Normalize();
            Look = position + direction;

            spherePoint.X = (float)Math.Atan2(direction.X, direction.Z);
            spherePoint.Y = (float)Math.Atan2(Math.Sqrt(Math.Pow(direction.X, 2) + Math.Pow(direction.Z, 2)), direction.Y);
        }

        /// <summary>
        /// Updates camera projection matrix.
        /// </summary>
        public void UpdateProjection()
        {
            Viewport viewport = graphicsDevice.ImmediateContext.Rasterizer.GetViewports()[0];
            projection = Matrix.PerspectiveFovLH(fieldOfView, viewport.Width / viewport.Height, nearPlane, farPlane);
            viewProjection = view * projection;
            ExtractPlanesFromFrustum();
        }

        /// <summary>
        /// Moves camera look vector around a sphere.
        /// </summary>
        /// <param name="mousePositionDelta">Mouse position change.</param>
        /// <param name="speed">Desired moving speed.</param>
        public void MoveLook(Vector2 mousePositionDelta, float speed)
        {
            spherePoint.X += mousePositionDelta.X * speed / 100;
            spherePoint.Y += mousePositionDelta.Y * speed / 100;

            // Prevents from image disappearance.
            if (spherePoint.Y < 0.001f)
                spherePoint.Y = 0.001f;
            if (spherePoint.Y > Math.PI - 0.001f)
                spherePoint.Y = (float)Math.PI - 0.001f;

            Vector3 newLook = new Vector3
            {
                X = (float)(Math.Sin(spherePoint.Y) * Math.Sin(spherePoint.X)),
                Y = (float)Math.Cos(spherePoint.Y),
                Z = (float)(Math.Sin(spherePoint.Y) * Math.Cos(spherePoint.X))
            };

            Look = position + newLook;
        }

        /// <summary>
        /// Moves a camera position vector.
        /// </summary>
        /// <param name="moveDirection">Movement direction.</param>
        /// <param name="deltaTime">Performance timer delta time.</param>
        public void MovePosition(MoveDirection moveDirection, double deltaTime)
        {
            Viewport viewport = graphicsDevice.ImmediateContext.Rasterizer.GetViewports()[0];
            Vector3 direction = Helper.MouseDirection(graphicsDevice, this, new Vector2(viewport.Width / 2, viewport.Height / 2));

            switch (moveDirection)
            {
                case MoveDirection.Forward:
                    direction *= Speed * (float)deltaTime;
                    break;
                case MoveDirection.Backward:
                    direction *= -Speed * (float)deltaTime;
                    break;
                case MoveDirection.Left:
                    direction.Y = 0;
                    direction.Normalize();
                    direction = Vector3.Cross(direction, up);
                    direction *= Speed * (float)deltaTime;
                    break;
                case MoveDirection.Right:
                    direction.Y = 0;
                    direction.Normalize();
                    direction = Vector3.Cross(up, direction);
                    direction *= Speed * (float)deltaTime;
                    break;
            }

            position += direction;
            Look += direction;
        }
    }
}
