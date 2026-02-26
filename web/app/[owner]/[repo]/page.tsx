import { redirect } from "next/navigation";
import { fetchRepoTree } from "@/lib/repository-api";
import { DocNotFound } from "@/components/repo/doc-not-found";
import { DocsPage, DocsBody } from "fumadocs-ui/page";

interface RepoIndexProps {
  params: Promise<{
    owner: string;
    repo: string;
  }>;
}

function encodeSlug(slug: string) {
  return slug
    .split("/")
    .map((segment) => encodeURIComponent(segment))
    .join("/");
}

async function getTreeData(owner: string, repo: string) {
  try {
    return await fetchRepoTree(owner, repo);
  } catch {
    return null;
  }
}

export default async function RepoIndex({ params }: RepoIndexProps) {
  const { owner, repo } = await params;
  
  const tree = await getTreeData(owner, repo);
  
  // API错误，layout会处理
  if (!tree) {
    return null;
  }
  
  // 仓库不存在，layout会处理
  if (!tree.exists) {
    return null;
  }

  // 仓库正在处理中、等待处理或失败，layout会处理显示
  if (tree.statusName !== "Completed") {
    return null;
  }

  // 有默认文档，重定向
  if (tree.defaultSlug) {
    redirect(`/${owner}/${repo}/${encodeSlug(tree.defaultSlug)}`);
  }

  // 没有默认文档但有目录，显示提示
  if (tree.nodes.length > 0) {
    return (
      <DocsPage toc={[]}>
        <DocsBody>
          <DocNotFound slug="" />
        </DocsBody>
      </DocsPage>
    );
  }

  // 空仓库，layout会处理
  return null;
}
