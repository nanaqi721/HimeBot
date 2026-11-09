# 查看提交日志

```git
git log --oneline (按q退出)

git log --oneline --graph --decorate --all
```

# 撤销提交

```git

// 撤销 ddc9adf 的修改其他的保留
git revert --no-edit ddc9adf  

git revert --no-edit HEAD

// 回到 53d27c7 版本
git reset --hard 53d27c7
```

