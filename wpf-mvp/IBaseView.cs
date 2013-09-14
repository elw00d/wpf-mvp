namespace Wpf.Mvp
{
    /// <summary>
    /// Base interface of all view classes.
    /// This is empty interface, if you need to add some specific functionality to your view interface,
    /// you should declare your own interface derived from <see cref="IBaseView"/>.
    /// </summary>
	public interface IBaseView
	{
	}

    public interface ICloseableView : IBaseView
    {
        void Close();
    }
    
}
