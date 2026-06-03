using Terminal.Gui;

Application.Init();

var win = new Window("TP4 - Catalogo REST")
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var panelProductos = new FrameView("Productos")
{
    X = 0,
    Y = 0,
    Width = Dim.Percent(50),
    Height = Dim.Fill()
};

var panelMovimientos = new FrameView("Movimientos")
{
    X = Pos.Percent(50),
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var listaProductos = new ListView()
{
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

panelProductos.Add(listaProductos);

win.Add(panelProductos);
win.Add(panelMovimientos);

Application.Run(win);

Application.Shutdown();