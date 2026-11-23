interface Properties {
  title: string;
}

export default function PageName({ title }: Properties) {
  document.title = title;

  return null;
}
