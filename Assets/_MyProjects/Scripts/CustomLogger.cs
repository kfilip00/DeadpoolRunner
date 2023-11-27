using UnityEngine;


public static class CustomLogger
{
    public static void Log(string _context, Object _sender)
    {
      Color _color = Color.white;
      _context = $"----- <color={_color}>{_context}</color>";
        Debug.Log(_context,_sender);
    }
    
    public static void Log(string _context, Color _color)
    {
        _context = $"----- <color={_color}>{_context}</color>";
        Debug.Log(_context);
    }
    
    public static void Log(string _context)
    {
        Color _color = Color.white;
        _context = $"----- <color={_color}>{_context}</color>";
        Debug.Log(_context);
    }
}