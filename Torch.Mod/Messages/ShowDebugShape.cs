
using ProtoBuf;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;
using Color = VRageMath.Color;

namespace Torch.Mod.Messages
{
    [ProtoContract]
    public class DrawDebug : MessageBase
    {
        //Since we can only clear all renders, we can keep track of each one sent such that we can remove one but keep the rest
        public static Dictionary<string, DrawDebug> AllDraws = new Dictionary<string, DrawDebug>();


        //Used as an identifier if you only want to remove one draw group
        [ProtoMember(301)]
        public string uniqueName;

        [ProtoMember(302)]
        public List<drawObject> drawObjects = new List<drawObject>();


        [ProtoMember(303)]
        public bool remove = false;

        [ProtoMember(304)]
        public bool removeAll = false;



        public DrawDebug() { }


        public DrawDebug(string uniqueName)
        {
            this.uniqueName = uniqueName;
        }

        public override void ProcessClient()
        {
            //must have a unique name
            if (string.IsNullOrEmpty(uniqueName))
                return;

            if (remove)
            {
                clear(uniqueName);
            }
            else if (removeAll)
            {
                clearAll();
            }

            //MyAPIGateway.Utilities.ShowMessage("Torch", $"Hit process client on debug draw!");

            //If its the same unique name, we can add it
            if(AllDraws.ContainsKey(uniqueName))
            {
                AllDraws[uniqueName].drawObjects.AddList(drawObjects);
            }
            else
            {
                AllDraws.Add(uniqueName, this);
            }
                

            //If both are not false, then we have new items to draw
        }

        public override void ProcessServer()
        {
            //Nothing to do server side
        }



        public void addOBB(BoundingBoxD box, Vector3D position, Vector3D forward, Vector3D up, Color color, MySimpleObjectRasterizer raster, float intensity, float linethickness)
        {
            drawObject obj = new drawObject(drawObject.drawtype.OBB);

            obj.box = box;
            obj.position = position;
            obj.forward = forward;
            obj.up = up;
            obj.color = color;
            obj.intensity = intensity;
            obj.linethickness = linethickness;
            obj.raster = raster;

            drawObjects.Add(obj);
        }

        public void addOBBLinkedEntity(long entityID, Color color, MySimpleObjectRasterizer raster, float intensity, float linethickness)
        {
            drawObject obj = new drawObject(drawObject.drawtype.OBBEntity);

            obj.entityID = entityID;
            obj.color = color;
            obj.intensity = intensity;
            obj.linethickness = linethickness;
            obj.raster = raster;

            drawObjects.Add(obj);
        }

        public void addSphere(Vector3D position, float radius, Color color, MySimpleObjectRasterizer raster, float intensity, float linethickness)
        {
            drawObject obj = new drawObject(drawObject.drawtype.Sphere);

            obj.position = position;
            obj.radius = radius;
            obj.color = color;
            obj.intensity = intensity;
            obj.linethickness = linethickness;
            obj.raster = raster;

            drawObjects.Add(obj);
        }

        


        public static void clear(string uniquename)
        {
            if (AllDraws.ContainsKey(uniquename))
                AllDraws.Remove(uniquename);
        }

        public static void clearAll()
        {
            AllDraws.Clear();
        }

        //Updates individual method
        public void refreshDraw()
        {
            foreach (var draw in drawObjects)
            {
                draw.update();
            }
        }


        //static update to update all draws
        public static void refreshAllDraws()
        {
            try
            {

                foreach (var draw in AllDraws)
                {
                    draw.Value.refreshDraw();
                }

            }catch(Exception ex)
            {
                //do nothings
            }
        }

    }


    [ProtoContract]
    public class drawObject
    {

        public MyEntity entRef;
        public int searchAttempts = 0;

        public drawObject() { }

        public drawObject(drawtype type) { this.type = type; }



        [ProtoMember(100)]
        public drawtype type;

        [ProtoMember(101)]
        public Vector3D position;

        [ProtoMember(102)]
        public VRageMath.Color color;

        [ProtoMember(103)]
        public MySimpleObjectRasterizer raster;

        [ProtoMember(104)]
        public BoundingBoxD box;

        [ProtoMember(105)]
        public Vector3D forward;

        [ProtoMember(106)]
        public Vector3D up;

        [ProtoMember(118)]
        public int wireDivideRatio = 0;

        [ProtoMember(120)]
        public float radius;

        [ProtoMember(121)]
        public float intensity;

        [ProtoMember(122)]
        public float linethickness = -1;

        [ProtoMember(123)]
        public long entityID = 0;

        //[ProtoMember(100)]
        //public MyOrientedBoundingBoxD obb;


        public enum drawtype
        {
            OBB,
            Sphere,
            OBBEntity,
        }

        public void update()
        {

            switch (type)
            {
                case drawtype.OBB:
                    drawOBB();
                    break;
                case drawtype.Sphere:
                    drawSphere();
                    break;
                case drawtype.OBBEntity:
                    drawOBBEntity();
                    break;
            }
        }


        public void drawOBB()
        {
            MatrixD worldMatrix = MatrixD.CreateWorld(position, forward, up);
            var material = TorchModCore.id;
            MySimpleObjectDraw.DrawTransparentBox(ref worldMatrix, ref box, ref color, raster, wireDivideRatio, linethickness, material, material, intensity: intensity);
        }

        public void drawSphere()
        {
            var material = TorchModCore.id;
            var transform = MatrixD.CreateTranslation(position);
            
            MySimpleObjectDraw.DrawTransparentSphere(ref transform, radius, ref color, raster, wireDivideRatio, material, material, linethickness, intensity: intensity);
        }

        public void drawOBBEntity()
        {
            //This will keep updating the draw for live grid box preview
            //Do not keep searching for entity on draw 
            if (searchAttempts > 250 || entityID == 0)
                return;


            


            entRef = MyEntities.GetEntityById(entityID);
            if (entRef == null )
            {
                searchAttempts++;
                return;
            }


            var material = TorchModCore.id;
            var Matrix = entRef.WorldMatrix;
            BoundingBoxD myAabb = entRef.PositionComp.LocalAABB;

            MySimpleObjectDraw.DrawTransparentBox(ref Matrix, ref myAabb, ref color, raster, wireDivideRatio, linethickness, material, material);
        }






    }

}
