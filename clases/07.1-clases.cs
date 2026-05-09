using static System.Console;

var p3 = Punto3D(10, 20, 40);

Mover(p3);
interface IPunto2D {
    int X {get;set;}
    int Y {get;set;}
}
void Mover(IPunto2D p){
    p.X += 1;
    p.Y += 1;
}


Punto p1 = null;
p1 = new Punto(100, 200);
// que valor tiene p1.x aca?

p1.X = 10;
p1.Mover(3,-10);
WriteLine($"Punto(x: {p1.X}, {p1.Y})");

class Punto : IPunto2D {
    public Punto(int x, int y) {
        this.x = x;
        this.y = y;
    }

    public int GetX() {
        return this.x;
    }
    public void SetX(int x)
    {
        if(x >= 0 || x <= 1024)
        {
            this.x = x;
        }
    }

    int x;
    public int X {
        public get
        {
            return this.x;
        }
        private set
        {
            this.x = value;
        }
    }
    public int Y() => this.y;

    public void Mover(int dx, int dy) {
        if(x < 0) { this.x = 0; }
        this.x += dx;
        this.y += dy;
    }

    int x;
    int y;
}


class Punto2D {
    Punto2D(int x, int y) {
        this.X = x;
        this.Y = y;
    }
    public int X {get;set;}
    public int Y {get;set;}

    int Distancia() {
        return Math.Sqrt(this.X ^ 2 + this.Y ^ 2);
    }
}

class Punto3D : Punto2D {
    Punto3D(int x, int y, int z) : base(x,y) {
        this.Z = z;
    }
    int Z {get;set;}

    int Distancia() {
        return Math.Sqrt(this.X ^ 2 + this.Y ^ 2 + this.Z ^ 2);
    }

}
