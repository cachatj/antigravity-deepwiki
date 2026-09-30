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
  
  // API error; handled by the layout
  if (!tree) {
    return null;
  }
  
  // Repository does not exist; handled by the layout
  if (!tree.exists) {
    return null;
  }

  // Repository is processing, pending, or failed; the layout handles the display
  if (tree.statusName !== "Completed") {
    return null;
  }

  // Default document exists; redirect
  if (tree.defaultSlug) {
    redirect(`/${owner}/${repo}/${encodeSlug(tree.defaultSlug)}`);
  }

  // No default document but a catalog exists; show a hint
  if (tree.nodes.length > 0) {
    return (
      <DocsPage toc={[]}>
        <DocsBody>
          <DocNotFound slug="" />
        </DocsBody>
      </DocsPage>
    );
  }

  // Empty repository; handled by the layout
  return null;
}
