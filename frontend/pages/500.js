import ErrorPage from "../components/errorPage";

const errorCode = 500;
const errorTitle = "Internal Server Error";
const errorDesc = "An unsigma error has happened and idk what it is lol";

export default () => <ErrorPage title={errorTitle} desc={errorDesc} code={errorCode} />
