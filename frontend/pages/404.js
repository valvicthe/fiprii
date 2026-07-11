import ErrorPage from "../components/errorPage";

const errorCode = 404;
const errorTitle = "This page dont exist bro";
const errorDesc = "Page Not fuckin found";

export default () => <ErrorPage title={errorTitle} desc={errorDesc} code={errorCode} />
