
using ProtoBuf;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
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
            if(string.IsNullOrEmpty(uniqueName)) 
                return;

            if(remove)
            {
                clear(uniqueName);
                return;
            }
            else if(removeAll)
            {
                clearAll();
                return;
            }

            //MyAPIGateway.Utilities.ShowMessage("Torch", $"Hit process client on debug draw!");

            if(!AllDraws.ContainsKey(uniqueName))
                AllDraws.Add(uniqueName, this);

            //If both are not false, then we have new items to draw
        }

        public override void ProcessServer()
        {
            //Nothing to do server side
        }



        public void addOBB(BoundingBoxD box, Quaternion orientation, Color color, MySimpleObjectRasterizer raster, float intensity, float linethickness)
        {
            drawObject obj = new drawObject(drawObject.drawtype.OBB);

            obj.box = box;
            obj.orientation = orientation;
            obj.color= color;
            obj.intensity = intensity;
            obj.linethickness = linethickness;
            obj.raster= raster;

            drawObjects.Add(obj);
        }

        public void addSphere(Vector3D position, float radius, Color color, MySimpleObjectRasterizer raster, float intensity, float linethickness)
        {
            drawObject obj = new drawObject(drawObject.drawtype.Sphere);

            obj.position = position;
            obj.radius = radius;
            obj.color= color;
            obj.intensity = intensity;
            obj.linethickness = linethickness;
            obj.raster= raster;

            drawObjects.Add(obj);
        }


        public static void clear(string uniquename)
        {
            if(AllDraws.ContainsKey(uniquename))
               AllDraws.Remove(uniquename);
        }

        public static void clearAll()
        {
            AllDraws.Clear();
        }

        //Updates individual method
        public void refreshDraw()
        {
            foreach(var draw in drawObjects)
            {
                draw.update();
            }
        }


        //static update to update all draws
        public static void refreshAllDraws()
        {
            //MyRenderProxy.DebugClearPersistentMessages();

            foreach (var draw in AllDraws)
            {
                draw.Value.refreshDraw();
            }
        }

    }

    
    [ProtoContract]
    public class drawObject
    {
   
        public drawObject() { }

        public drawObject(drawtype type) { this.type= type; }



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
        public Quaternion orientation;


        [ProtoMember(120)]
        public float radius;

        [ProtoMember(121)]
        public float intensity;

        [ProtoMember(122)]
        public float linethickness = -1;

        //[ProtoMember(100)]
        //public MyOrientedBoundingBoxD obb;


        public enum drawtype
        {
            OBB,
            Sphere
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
            }
        }


        public void drawOBB()
        {
            var transform = MatrixD.CreateFromQuaternion(orientation);
            var material = TorchModCore.id;
            MySimpleObjectDraw.DrawTransparentBox(ref transform, ref box, ref color, raster, 1, linethickness, material, material);

        }

        public void drawSphere()
        {
            var material = TorchModCore.id;
            var transform = MatrixD.CreateTranslation(position);
            MySimpleObjectDraw.DrawTransparentSphere(ref transform, 10, ref color, raster, 25, material, material, -1);
        }




    }
    
}
