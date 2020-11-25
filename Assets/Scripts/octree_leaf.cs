// ----------------------------------
// Namespace:   CSharpOctree
// Class:       OctreeLeaf
// Author:      Udo Schlegel
// Company:     DBVIS
// ----------------------------------

namespace CSharpOctree {

    public class OctreeLeaf
    {
        private object _obj;
        private double _x, _y, _z, _w;

        public OctreeLeaf(double x, double y, double z, double w, object obj)
        {
            this._x = x;
            this._y = y;
            this._z = z;
            this._w = w;

            this._obj = obj;
        }

        public string toString()
        {
            return "[" + this._x + ", " + this._y + ", " + this._z + ", " + this._w +"]";
        }

        public double getX()
        {
            return this._x;
        }

        public double getY()
        {
            return this._y;
        }

        public double getZ()
        {
            return this._z;
        }
        
        public double getW()
        {
            return this._w;
        }

        public object getObj()
        {
            return this._obj;
        }
    }
}
