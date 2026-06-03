using Terminal.Gui;

Application.Init();

var win = new Window("TP4 - Catalogo REST")
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var productos = new FrameView("Productos")
{
    X = 0,
    Y = 0,
    Width = Dim.Percent(50),
    Height = Dim.Fill()
};

var movimientos = new FrameView("Movimientos")
{
    X = Pos.Percent(50),
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

win.Add(productos);
win.Add(movimientos);

Application.Run(win);

Application.Shutdown();