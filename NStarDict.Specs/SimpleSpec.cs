using System.Threading.Tasks;

using PCLStorage;

using Xunit;

namespace NStarDict.Specs
{
    public class SimpleSpec
    {
        [Fact(Skip = "doesn't work")]
        public async Task TestAsync()
        {
            using (var dict = new StarDict(@"E:\Downloads\dict\stardict-freedict-eng-nld-2.4.2\dictd_www.freedict.de_eng-nld"))
            {
                await dict.Init();
            }
        }

        [Fact(Skip = "doesn't work")]
        public async Task TestLocalStorage()
        {
            IFolder rootFolder = FileSystem.Current.LocalStorage;
            await
                FileSystem.Current.LocalStorage.GetFileAsync(
                    @"E:\Downloads\dict\stardict-freedict-eng-nld-2.4.2\dictd_www.freedict.de_eng-nld.idx");
        } 
    }
}