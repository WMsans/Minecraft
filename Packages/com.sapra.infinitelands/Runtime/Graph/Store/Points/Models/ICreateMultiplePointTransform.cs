using System.Collections.Generic;

namespace sapra.InfiniteLands{
    public interface ICreateMultiplePointTransform : AwaitableData<List<PointTransform>>, IReusePointTransform{
    }
}