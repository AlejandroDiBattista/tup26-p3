
int x;
x = 10;

Punto p1 = new Punto(10);
p1.Mover(10,2);
p1.Mover(10);
p1.Mover();



p1.X = 100;
p1.Y = 200;

Console.WriteLine($"Punto({p1.X},{p1.Y}) {p1.Distancia }");
class Punto {
    int x;
    int y;

    public int Y;
    public int X;

    Punto(int x, int y) {
        this.x = x < 0 ? 0 : (x > 1024 ? 1024 : x);
        this.y = y < 0 ? 0 : (y > 768 ? 768 : y);
    }

    Punto(int x) : this(x, 0) { }
    Punto() : this(0, 0){ }

    public int Distancia => this.Distancia(new Punto());
    
    int Distancia(Punto otro) {
        return Math.Sqrt( Math.Pow(this.x - otro.x, 2) + Math.Pow(this.y - otro.y, 2) );
    }

    public int GetX() { return this.x; }
    public void SetX(int value)
    {
        if(value < 0) {
            this.x = 0;
        } else if (value > 1024) {
            this.x = 1024;
        } else {
            this.x = value;
        }
    }

    public void Mover(int dx=0, int dy=0)
    {
        this.SetX( this.GetX() + dx);

        this.y += dy;
    }

    public int X {
        get { return this.x; }
        set { this.x = value; }
    }
}

