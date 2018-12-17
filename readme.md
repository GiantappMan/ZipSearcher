# 搜索包含指定文本的zip文件

## 用例：
```
config test[名称] E:\***\ZipSearcher\Examples[压缩包路径] 3.txt[压缩包里面的文件路径]
sz hello  
sz 'xxx xxx'

---------
输出  
----E:\***\ZipSearcher\Examples\1\3.txt  
----E:\***\ZipSearcher\Examples\3\3.txt  
----E:\***\ZipSearcher\Examples\5\3.txt  
----E:\***\ZipSearcher\Examples\6\3.txt  
----找到4条记录
```
